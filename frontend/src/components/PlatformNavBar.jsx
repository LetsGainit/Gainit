import { Link } from "react-router-dom";
import "../css/PlatformNavBar.css";

function PlatformNavBar() {
  return (
    <nav className="platform-NavBar">
      <div>
        <Link to="/" className="platform-brand">
          Gainit
        </Link>
      </div>
    </nav>
  );
}

export default PlatformNavBar;
