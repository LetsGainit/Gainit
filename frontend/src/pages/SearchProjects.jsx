import ProjectOption from "../components/ProjectOption";
import "../css/PlatformNavBar.css";
import "../css/ProjectOption.css";

function SearchProjects() {
  const options = [
    {
      title: "Nonprofit Project",
      description:
        "Collaborate with a real nonprofit and build something that makes an impact.",
      image: "/NonprofitPhoto.png",
    },
    {
      title: "Learning Project",
      description:
        "Sharpen your skills by working on a guided project designed for growth.",
      image: "/LearningPhoto.png",
    },
  ];

  return (
    <div className="project-options-section">
      <div className="project-options-wrapper">
        {options.map((option, index) => (
          <ProjectOption key={index} option={option} />
        ))}
      </div>
    </div>
  );
}

export default SearchProjects;