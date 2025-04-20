import "./css/App.css";
import ProjectCard from "./components/ProjectCard";
import Home from "./pages/home";
import SearchProjects from "./pages/SearchProjects";
import { Routes, Route, useLocation } from "react-router-dom";
import NavBar from "./components/NavigatorBar";
import About from "./pages/About";
import PlatformNavBar from "./components/PlatformNavBar";

function App() {
  const location = useLocation();
  const isSearchPage = location.pathname === "/Search-Projects";

  return (
    <div>
      {isSearchPage ? <PlatformNavBar /> : <NavBar />}

      {isSearchPage ? (
        <Routes>
          <Route path="/Search-Projects" element={<SearchProjects />} />
        </Routes>
      ) : (
        <main className="main-content">
          <Routes>
            <Route path="/" element={<Home />} />
            <Route path="/about" element={<About />} />
          </Routes>
        </main>
      )}
    </div>
  );
}

export default App;
