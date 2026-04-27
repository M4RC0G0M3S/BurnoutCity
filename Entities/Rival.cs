namespace BurnoutCity.Entities
{
    public class Rival
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "Rival";
        public string CarName { get; set; } = "Unknown";
        public Color CarColor { get; set; } = Color.DeepSkyBlue;
        public float MaxSpeed { get; set; } = 350f;
        public float Acceleration { get; set; } = 140f;
        public int MinLevel { get; set; } = 1;
        public int BonusReward { get; set; } = 0;
        public string PreRaceQuote { get; set; } = "...";
        public bool IsDefeated { get; set; } = false;
    }
}