# SignalR Frontend Integration Guide

## Overview
This guide explains how to integrate SignalR real-time notifications in your frontend application, specifically for the bell icon notification system.

## Backend SignalR Events Reference

### Available Events
Based on your backend implementation, here are all the SignalR events you can listen to:

#### Project Events
```javascript
// Join request events
"projectJoinRequested"    // When someone requests to join a project
"projectJoinApproved"     // When your join request is approved
"projectJoinRejected"     // When your join request is rejected
"projectJoinCancelled"    // When a join request is cancelled
```

#### Task Events
```javascript
"taskCreated"             // When a new task is created
"taskCompleted"           // When a task is completed
"taskUnblocked"           // When a blocked task becomes available
"milestoneCompleted"      // When a milestone is completed
```

#### Forum Events
```javascript
"postReplied"             // When someone replies to your post
"postLiked"               // When someone likes your post
"replyLiked"              // When someone likes your reply
```

## Frontend Implementation

### 1. Install Required Packages

```bash
npm install @microsoft/signalr
# or
yarn add @microsoft/signalr
```

### 2. Create SignalR Service

Create a new file: `src/services/signalRService.js` (or `.ts` if using TypeScript)

```javascript
import * as signalR from '@microsoft/signalr';

class SignalRService {
    constructor() {
        this.connection = null;
        this.isConnected = false;
        this.eventHandlers = new Map();
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 5;
    }

    // Initialize connection
    async startConnection() {
        try {
            // Get your JWT token (adjust based on how you store it)
            const token = localStorage.getItem('access_token') || 
                         sessionStorage.getItem('access_token') ||
                         this.getTokenFromAuthProvider();

            if (!token) {
                console.warn('No JWT token found for SignalR connection');
                return false;
            }

            // Create connection
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl('/hubs/notifications', {
                    accessTokenFactory: () => token,
                    transport: signalR.HttpTransportType.WebSockets,
                    skipNegotiation: true
                })
                .withAutomaticReconnect([0, 2000, 10000, 30000]) // Auto-reconnect with backoff
                .configureLogging(signalR.LogLevel.Information)
                .build();

            // Set up event handlers
            this.setupEventHandlers();

            // Start connection
            await this.connection.start();
            this.isConnected = true;
            this.reconnectAttempts = 0;

            console.log('SignalR connected successfully');
            return true;

        } catch (error) {
            console.error('SignalR connection failed:', error);
            this.handleConnectionError(error);
            return false;
        }
    }

    // Set up all event handlers
    setupEventHandlers() {
        // Project Events
        this.connection.on('projectJoinRequested', (data) => {
            this.handleEvent('projectJoinRequested', data);
        });

        this.connection.on('projectJoinApproved', (data) => {
            this.handleEvent('projectJoinApproved', data);
        });

        this.connection.on('projectJoinRejected', (data) => {
            this.handleEvent('projectJoinRejected', data);
        });

        this.connection.on('projectJoinCancelled', (data) => {
            this.handleEvent('projectJoinCancelled', data);
        });

        // Task Events
        this.connection.on('taskCreated', (data) => {
            this.handleEvent('taskCreated', data);
        });

        this.connection.on('taskCompleted', (data) => {
            this.handleEvent('taskCompleted', data);
        });

        this.connection.on('taskUnblocked', (data) => {
            this.handleEvent('taskUnblocked', data);
        });

        this.connection.on('milestoneCompleted', (data) => {
            this.handleEvent('milestoneCompleted', data);
        });

        // Forum Events
        this.connection.on('postReplied', (data) => {
            this.handleEvent('postReplied', data);
        });

        this.connection.on('postLiked', (data) => {
            this.handleEvent('postLiked', data);
        });

        this.connection.on('replyLiked', (data) => {
            this.handleEvent('replyLiked', data);
        });

        // Connection state events
        this.connection.onclose((error) => {
            this.isConnected = false;
            console.log('SignalR connection closed:', error);
            this.handleConnectionError(error);
        });

        this.connection.onreconnecting((error) => {
            this.isConnected = false;
            console.log('SignalR reconnecting:', error);
        });

        this.connection.onreconnected((connectionId) => {
            this.isConnected = true;
            this.reconnectAttempts = 0;
            console.log('SignalR reconnected:', connectionId);
        });
    }

    // Generic event handler
    handleEvent(eventName, data) {
        console.log(`SignalR event received: ${eventName}`, data);
        
        // Call registered handlers
        const handlers = this.eventHandlers.get(eventName) || [];
        handlers.forEach(handler => {
            try {
                handler(data);
            } catch (error) {
                console.error(`Error in event handler for ${eventName}:`, error);
            }
        });

        // Trigger global notification system
        this.triggerNotification(eventName, data);
    }

    // Register event handler
    on(eventName, handler) {
        if (!this.eventHandlers.has(eventName)) {
            this.eventHandlers.set(eventName, []);
        }
        this.eventHandlers.get(eventName).push(handler);
    }

    // Remove event handler
    off(eventName, handler) {
        const handlers = this.eventHandlers.get(eventName);
        if (handlers) {
            const index = handlers.indexOf(handler);
            if (index > -1) {
                handlers.splice(index, 1);
            }
        }
    }

    // Handle connection errors
    handleConnectionError(error) {
        this.reconnectAttempts++;
        
        if (this.reconnectAttempts < this.maxReconnectAttempts) {
            console.log(`Attempting to reconnect... (${this.reconnectAttempts}/${this.maxReconnectAttempts})`);
            setTimeout(() => {
                this.startConnection();
            }, Math.pow(2, this.reconnectAttempts) * 1000); // Exponential backoff
        } else {
            console.error('Max reconnection attempts reached');
        }
    }

    // Get token from your auth provider (adjust based on your setup)
    getTokenFromAuthProvider() {
        // Example: if using MSAL
        // return msalInstance.getActiveAccount()?.idToken;
        
        // Example: if using Auth0
        // return auth0Client.getTokenSilently();
        
        // Example: if using custom auth
        // return yourAuthService.getToken();
        
        return null;
    }

    // Disconnect
    async stopConnection() {
        if (this.connection) {
            await this.connection.stop();
            this.isConnected = false;
            console.log('SignalR disconnected');
        }
    }

    // Check connection status
    getConnectionState() {
        return this.connection?.state || signalR.HubConnectionState.Disconnected;
    }

    // Trigger notification (implement based on your notification system)
    triggerNotification(eventName, data) {
        // This will be implemented in the notification component
        window.dispatchEvent(new CustomEvent('signalr-notification', {
            detail: { eventName, data }
        }));
    }
}

// Export singleton instance
export const signalRService = new SignalRService();
export default signalRService;
```

### 3. Create Notification Component

Create a new file: `src/components/NotificationBell.jsx` (or `.tsx`)

```jsx
import React, { useState, useEffect, useRef } from 'react';
import { Bell, BellRing, X, CheckCircle, AlertCircle, Info, Users, CheckSquare, MessageSquare } from 'lucide-react';
import signalRService from '../services/signalRService';

const NotificationBell = () => {
    const [notifications, setNotifications] = useState([]);
    const [isOpen, setIsOpen] = useState(false);
    const [unreadCount, setUnreadCount] = useState(0);
    const notificationRef = useRef(null);

    useEffect(() => {
        // Start SignalR connection when component mounts
        signalRService.startConnection();

        // Listen for SignalR notifications
        const handleNotification = (event) => {
            const { eventName, data } = event.detail;
            addNotification(eventName, data);
        };

        window.addEventListener('signalr-notification', handleNotification);

        // Cleanup on unmount
        return () => {
            window.removeEventListener('signalr-notification', handleNotification);
            signalRService.stopConnection();
        };
    }, []);

    // Add new notification
    const addNotification = (eventName, data) => {
        const notification = {
            id: Date.now() + Math.random(),
            type: getNotificationType(eventName),
            title: getNotificationTitle(eventName, data),
            message: getNotificationMessage(eventName, data),
            timestamp: new Date(),
            read: false,
            eventName,
            data
        };

        setNotifications(prev => [notification, ...prev]);
        setUnreadCount(prev => prev + 1);

        // Auto-remove after 10 seconds
        setTimeout(() => {
            removeNotification(notification.id);
        }, 10000);
    };

    // Get notification type based on event
    const getNotificationType = (eventName) => {
        if (eventName.includes('Approved') || eventName.includes('Completed')) {
            return 'success';
        } else if (eventName.includes('Rejected') || eventName.includes('Error')) {
            return 'error';
        } else if (eventName.includes('Requested') || eventName.includes('Created')) {
            return 'info';
        } else {
            return 'info';
        }
    };

    // Get notification title
    const getNotificationTitle = (eventName, data) => {
        const titles = {
            'projectJoinRequested': 'New Join Request',
            'projectJoinApproved': 'Join Request Approved',
            'projectJoinRejected': 'Join Request Rejected',
            'projectJoinCancelled': 'Join Request Cancelled',
            'taskCreated': 'New Task Created',
            'taskCompleted': 'Task Completed',
            'taskUnblocked': 'Task Unblocked',
            'milestoneCompleted': 'Milestone Completed',
            'postReplied': 'New Reply',
            'postLiked': 'Post Liked',
            'replyLiked': 'Reply Liked'
        };
        return titles[eventName] || 'New Notification';
    };

    // Get notification message
    const getNotificationMessage = (eventName, data) => {
        switch (eventName) {
            case 'projectJoinRequested':
                return `Someone requested to join project "${data.projectName || 'Unknown Project'}"`;
            case 'projectJoinApproved':
                return `Your request to join project has been approved`;
            case 'projectJoinRejected':
                return `Your request to join project was rejected${data.decisionReason ? ': ' + data.decisionReason : ''}`;
            case 'projectJoinCancelled':
                return `A join request for project has been cancelled`;
            case 'taskCreated':
                return `New task "${data.title}" created in project "${data.projectName}"`;
            case 'taskCompleted':
                return `Task "${data.title}" has been completed in project "${data.projectName}"`;
            case 'taskUnblocked':
                return `Task "${data.title}" is now unblocked and ready to work on`;
            case 'milestoneCompleted':
                return `Milestone "${data.title}" completed in project "${data.projectName}"`;
            case 'postReplied':
                return `${data.replyAuthorName} replied to your post in project "${data.projectName}"`;
            case 'postLiked':
                return `${data.likedByUserName} liked your post in project "${data.projectName}"`;
            case 'replyLiked':
                return `${data.likedByUserName} liked your reply in project "${data.projectName}"`;
            default:
                return 'You have a new notification';
        }
    };

    // Get notification icon
    const getNotificationIcon = (type, eventName) => {
        if (type === 'success') return <CheckCircle className="w-5 h-5 text-green-500" />;
        if (type === 'error') return <AlertCircle className="w-5 h-5 text-red-500" />;
        
        // Event-specific icons
        if (eventName.includes('project') || eventName.includes('Join')) {
            return <Users className="w-5 h-5 text-blue-500" />;
        } else if (eventName.includes('task') || eventName.includes('milestone')) {
            return <CheckSquare className="w-5 h-5 text-purple-500" />;
        } else if (eventName.includes('post') || eventName.includes('reply')) {
            return <MessageSquare className="w-5 h-5 text-orange-500" />;
        }
        
        return <Info className="w-5 h-5 text-blue-500" />;
    };

    // Mark notification as read
    const markAsRead = (id) => {
        setNotifications(prev => 
            prev.map(notification => 
                notification.id === id 
                    ? { ...notification, read: true }
                    : notification
            )
        );
        setUnreadCount(prev => Math.max(0, prev - 1));
    };

    // Mark all as read
    const markAllAsRead = () => {
        setNotifications(prev => 
            prev.map(notification => ({ ...notification, read: true }))
        );
        setUnreadCount(0);
    };

    // Remove notification
    const removeNotification = (id) => {
        setNotifications(prev => {
            const notification = prev.find(n => n.id === id);
            if (notification && !notification.read) {
                setUnreadCount(prev => Math.max(0, prev - 1));
            }
            return prev.filter(n => n.id !== id);
        });
    };

    // Handle notification click
    const handleNotificationClick = (notification) => {
        markAsRead(notification.id);
        
        // Navigate based on notification type
        switch (notification.eventName) {
            case 'projectJoinRequested':
                // Navigate to project join requests page
                window.location.href = `/projects/${notification.data.projectId}/join-requests`;
                break;
            case 'taskCreated':
            case 'taskCompleted':
            case 'taskUnblocked':
                // Navigate to project tasks page
                window.location.href = `/projects/${notification.data.projectId}/tasks`;
                break;
            case 'postReplied':
            case 'postLiked':
            case 'replyLiked':
                // Navigate to project forum
                window.location.href = `/projects/${notification.data.projectId}/forum`;
                break;
            default:
                // Navigate to project overview
                window.location.href = `/projects/${notification.data.projectId}`;
        }
    };

    // Format timestamp
    const formatTimestamp = (timestamp) => {
        const now = new Date();
        const diff = now - timestamp;
        const minutes = Math.floor(diff / 60000);
        const hours = Math.floor(diff / 3600000);
        const days = Math.floor(diff / 86400000);

        if (minutes < 1) return 'Just now';
        if (minutes < 60) return `${minutes}m ago`;
        if (hours < 24) return `${hours}h ago`;
        return `${days}d ago`;
    };

    return (
        <div className="relative">
            {/* Bell Icon */}
            <button
                onClick={() => setIsOpen(!isOpen)}
                className="relative p-2 text-gray-600 hover:text-gray-900 focus:outline-none focus:ring-2 focus:ring-blue-500 rounded-full"
            >
                {unreadCount > 0 ? (
                    <BellRing className="w-6 h-6 text-blue-600" />
                ) : (
                    <Bell className="w-6 h-6" />
                )}
                
                {/* Unread Count Badge */}
                {unreadCount > 0 && (
                    <span className="absolute -top-1 -right-1 bg-red-500 text-white text-xs rounded-full h-5 w-5 flex items-center justify-center">
                        {unreadCount > 99 ? '99+' : unreadCount}
                    </span>
                )}
            </button>

            {/* Notification Dropdown */}
            {isOpen && (
                <div className="absolute right-0 mt-2 w-80 bg-white rounded-lg shadow-lg border border-gray-200 z-50">
                    {/* Header */}
                    <div className="p-4 border-b border-gray-200">
                        <div className="flex items-center justify-between">
                            <h3 className="text-lg font-semibold text-gray-900">
                                Notifications
                            </h3>
                            <div className="flex items-center space-x-2">
                                {unreadCount > 0 && (
                                    <button
                                        onClick={markAllAsRead}
                                        className="text-sm text-blue-600 hover:text-blue-800"
                                    >
                                        Mark all read
                                    </button>
                                )}
                                <button
                                    onClick={() => setIsOpen(false)}
                                    className="text-gray-400 hover:text-gray-600"
                                >
                                    <X className="w-4 h-4" />
                                </button>
                            </div>
                        </div>
                    </div>

                    {/* Notifications List */}
                    <div className="max-h-96 overflow-y-auto">
                        {notifications.length === 0 ? (
                            <div className="p-4 text-center text-gray-500">
                                No notifications yet
                            </div>
                        ) : (
                            notifications.map((notification) => (
                                <div
                                    key={notification.id}
                                    className={`p-4 border-b border-gray-100 hover:bg-gray-50 cursor-pointer ${
                                        !notification.read ? 'bg-blue-50' : ''
                                    }`}
                                    onClick={() => handleNotificationClick(notification)}
                                >
                                    <div className="flex items-start space-x-3">
                                        {getNotificationIcon(notification.type, notification.eventName)}
                                        <div className="flex-1 min-w-0">
                                            <div className="flex items-center justify-between">
                                                <p className="text-sm font-medium text-gray-900">
                                                    {notification.title}
                                                </p>
                                                <button
                                                    onClick={(e) => {
                                                        e.stopPropagation();
                                                        removeNotification(notification.id);
                                                    }}
                                                    className="text-gray-400 hover:text-gray-600"
                                                >
                                                    <X className="w-3 h-3" />
                                                </button>
                                            </div>
                                            <p className="text-sm text-gray-600 mt-1">
                                                {notification.message}
                                            </p>
                                            <p className="text-xs text-gray-400 mt-1">
                                                {formatTimestamp(notification.timestamp)}
                                            </p>
                                        </div>
                                    </div>
                                </div>
                            ))
                        )}
                    </div>

                    {/* Footer */}
                    {notifications.length > 0 && (
                        <div className="p-4 border-t border-gray-200">
                            <button
                                onClick={() => {
                                    setNotifications([]);
                                    setUnreadCount(0);
                                }}
                                className="w-full text-sm text-gray-600 hover:text-gray-800"
                            >
                                Clear all notifications
                            </button>
                        </div>
                    )}
                </div>
            )}
        </div>
    );
};

export default NotificationBell;
```

### 4. Integration in Your App

Add the notification bell to your main layout or header:

```jsx
// In your main layout component
import NotificationBell from './components/NotificationBell';

const Layout = ({ children }) => {
    return (
        <div className="min-h-screen bg-gray-50">
            {/* Header */}
            <header className="bg-white shadow-sm border-b border-gray-200">
                <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
                    <div className="flex justify-between items-center h-16">
                        {/* Your logo/brand */}
                        <div className="flex items-center">
                            <h1 className="text-xl font-bold">GainIt</h1>
                        </div>

                        {/* Navigation and Notification Bell */}
                        <div className="flex items-center space-x-4">
                            {/* Your navigation items */}
                            <nav className="hidden md:flex space-x-8">
                                <a href="/projects" className="text-gray-600 hover:text-gray-900">
                                    Projects
                                </a>
                                <a href="/tasks" className="text-gray-600 hover:text-gray-900">
                                    Tasks
                                </a>
                            </nav>

                            {/* Notification Bell */}
                            <NotificationBell />
                        </div>
                    </div>
                </div>
            </header>

            {/* Main Content */}
            <main className="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
                {children}
            </main>
        </div>
    );
};

export default Layout;
```

### 5. Environment Configuration

Make sure your frontend can connect to the correct SignalR endpoint:

```javascript
// In your environment configuration
const config = {
    development: {
        signalRUrl: 'http://localhost:5000/hubs/notifications',
        apiUrl: 'http://localhost:5000/api'
    },
    production: {
        signalRUrl: 'https://your-api-domain.com/hubs/notifications',
        apiUrl: 'https://your-api-domain.com/api'
    }
};
```

## Testing the Integration

### 1. Test Connection
```javascript
// In browser console
console.log('SignalR State:', signalRService.getConnectionState());
```

### 2. Test Notifications
Create a task or join request in your app and watch for real-time notifications.

### 3. Check Network Tab
Look for WebSocket connection to `/hubs/notifications` in your browser's Network tab.

## Troubleshooting

### Common Issues:

1. **Connection Failed**: Check if JWT token is valid and not expired
2. **No Notifications**: Verify user has correct `ExternalId` in database
3. **CORS Issues**: Ensure your frontend domain is in the CORS policy
4. **Authentication Issues**: Check if JWT token is being passed correctly

### Debug Mode:
```javascript
// Enable detailed logging
signalRService.connection.configureLogging(signalR.LogLevel.Debug);
```

## Security Considerations

1. **Token Storage**: Store JWT tokens securely (httpOnly cookies recommended for production)
2. **Token Refresh**: Implement token refresh logic for long-lived connections
3. **Connection Cleanup**: Always disconnect SignalR when user logs out
4. **Rate Limiting**: Consider implementing rate limiting for notifications

This implementation provides a complete, production-ready notification system that integrates seamlessly with your existing SignalR backend!
