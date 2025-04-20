import { Link } from "react-router-dom";
import '../css/NavigatorBar.css';

function NavBar() {
    return ( 
    <nav className="NavBar">
        <div className="navbar-brand">
            <Link to="/">Gainit</Link>
        </div>
        <div className="navbar-links">
            <Link to="/Search-Projects" className="nav-link">Find Project</Link>
            <Link to="/about" className="nav-link">About</Link>
        </div>
    </nav>
    );
}

export default NavBar