using UnityEngine;

public class ResourceBuilding : Building
{
    public int ResourceGenerationRate { get; set; }
    private float _generationTimer = 0f;

    public override void GameUpdate(float deltaTime)
    {
        if (CurrentState == BuildingState.Active)
        {
            _generationTimer += deltaTime;
            if (_generationTimer >= 1f)
            {
                ResourceManager.Instance?.AddResource(Team, ResourceGenerationRate);
                _generationTimer = 0f;
            }
        }
    }
}