namespace BurnoutCity.Entities
{
    public class CarStats
    {
        public float MaxSpeed { get; set; } = 300f;        // Velocidade máxima em unidades/segundo
        public float Acceleration { get; set; } = 200f;    // Aceleração em unidades/segundo²
        public float Handling { get; set; } = 1.0f;        // Multiplicador de viragem (0.5 = metade, 2.0 = dobro)
        public float NitroBoost { get; set; } = 150f;      // Boost adicional quando nitro activo
        public float CurrentDamage { get; set; } = 0f;     // Dano actual do carro (0 = sem dano, 100 = completamente destruído)
    }
}