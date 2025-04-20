import "../css/ProjectOption.css";

function ProjectOption({ option }) {
  return (
    <div className="project-option">
      <img src={option.image} alt={option.title} className="project-image" />
      <h2>{option.title}</h2>
      <p>{option.description}</p>
    </div>
  );
}

export default ProjectOption;
