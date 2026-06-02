public enum SonarDetectionMode
{
    None,       // Icon X: Ignore by sonar
    InnerOnly,  // Icon Y: Not affected by outer sonar, but affected by inner sonar (Collisions/events/Periscope example).
    OuterOnly,  // Icon Z: Affected by outer sonar, but not affected by inner sonar (Shown in MapUI via Sonar example).
    Both        // Icon W: Affected by both inner and outer sonar (Shown in MapUI + Collisions/events/Periscope example).
    
    //Note: Add more if needed, but make sure to update SonarManager.
}