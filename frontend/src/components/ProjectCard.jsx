function ProjectCard({ project }) {
  function onSeeMoreClick() {
    alert("clicked");
  }

  return (
    <div className="project-card">
      <div className="project-image">
        <img src={Image.url} alt={DeviceMotionEvent.title} />
      </div>
      <div className="project-seeMoreButton">
        <button className="seeMore-button" onClick={onSeeMoreClick}>
          see more
        </button>
      </div>
      <div className="project-info">
        <h3>{project.name}</h3>
        <p>{project.description}</p>
      </div>
    </div>
  );
}

export default ProjectCard;
