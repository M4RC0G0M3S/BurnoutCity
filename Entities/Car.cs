using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Reflection.Metadata;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace BurnoutCity.Entities
{
    public class Car
    {
        public Vector2  Position { get; private set; }
        public float Rotation { get; private set; }

        public Rectangle Bounds => GetBounds();

        private Vector2 _velocity;
        private float _speed;

        public CarStats Stats { get; private set; }

        //Visual Temporario Mudar Depois com os spites
        public Color CarColor { get; set; } = Color.OrangeRed;
        private const int CarWidth = 24;
        private const int CarHeight = 44;

        private bool _isAccelerating; // Flag para controlar a aceleração

        //Fisicas

        private const float Friction = 0.88f; // Fator de atrito para desacelerar o carro quando não estiver acelerado
        private const float TurnSpeed = 2.8f; // Velocidade de rotação do carro
        private const float MinSpeedToTurn = 10f; // Velocidade mínima para permitir a rotação do carro

        //constructor
        public Car(Vector2 spawnpoint, CarStats? stats = null)
        {
            Position = spawnpoint;
            Stats = stats ?? new CarStats();
            Rotation = 0f;
            _velocity = Vector2.Zero;
            _speed = 0f;
        }

        //Updates / Ler Inputs

        public void Update(GameTime gameTime)
        {
            float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
            KeyboardState keyboard = Keyboard.GetState();

            HandleInput(keyboard, delta);
            ApplyFriction();
            ApplyMovement(delta);
        }

        private void HandleInput(KeyboardState keyboard, float delta)
        {
            bool accelerating = keyboard.IsKeyDown(Keys.W); 
            bool braking = keyboard.IsKeyDown(Keys.S);
            bool turningLeft = keyboard.IsKeyDown(Keys.A); 
            bool turningRight = keyboard.IsKeyDown(Keys.D);

            if (accelerating)  // Se a tecla de aceleração estiver pressionada, aumente a velocidade do carro
            {
                float effectiveMaxSpeed = Stats.MaxSpeed * getDamageSpeedMultiplier(); // Calcula a velocidade máxima efetiva com base no dano atual do carro
                if (_speed < effectiveMaxSpeed) // Verifica se a velocidade atual é menor que a velocidade máxima efetiva antes de acelerar
                {
                    _speed += Stats.Acceleration * delta;
                    _speed = MathHelper.Min(_speed, effectiveMaxSpeed);

                }
            }
            if (braking)
            {
                if (_speed > 0f)
                {
                    _speed -= Stats.Acceleration * 1.5f * delta; // Aplique uma desaceleração mais forte ao Travar
                    _speed = MathHelper.Max(_speed, 0f); // Certifique-se de que a velocidade não fique negativa
                }
            }
            else
            {
                _speed -= Stats.Acceleration * 0.5f * delta; // Desaceleração natural quando não estiver a acelerar ou a travar
                _speed = MathHelper.Max(_speed, -Stats.MaxSpeed * 0.4f); // Permitir uma velocidade reversa limitada    
            }

            if(Math.Abs(_speed) > MinSpeedToTurn) // Permitir a rotação apenas se a velocidade for suficiente para evitar que o carro gire no lugar
            {
                float speedratio = Math.Abs(_speed) / Stats.MaxSpeed; // Calcula a proporção da velocidade atual em relação à velocidade máxima para ajustar a velocidade de rotação
                float currentTurnSpeed = TurnSpeed * Stats.Handling * speedratio * delta; // Ajusta a velocidade de rotação com base na proporção da velocidade e no manuseio do carro
            
                if (turningLeft)
                {
                    Rotation -= currentTurnSpeed; // Gira o carro para a esquerda
                }
                if (turningRight)
                {
                    Rotation += currentTurnSpeed; // Gira o carro para a direita
                }

                Rotation = NormalizeAngle(Rotation); // Normaliza o ângulo para manter a rotação dentro de um intervalo de 0 a 360 graus

                _isAccelerating = accelerating || braking; // Define a flag de aceleração com base nas teclas de aceleração ou travagem

            }       
                   
    
        }  
        private void ApplyFriction()
        {
            if (!_isAccelerating) // Aplique o atrito apenas quando o carro não estiver acelerando ou travando
            {
                _speed *= Friction; // Reduz a velocidade do carro multiplicando pela constante de atrito

                if (Math.Abs(_speed) < 0.5f) // Se a velocidade for muito baixa, pare completamente o carro para evitar movimento residual
                {
                    _speed = 0f;
                }
            }
        } 

        private void ApplyMovement(float delta)
        {
            if(_speed == 0f) return; // Se a velocidade for zero, não aplique movimento para evitar cálculos desnecessários
            _velocity = new Vector2(
                MathF.Sin(Rotation) * _speed, // Calcula a componente X da velocidade com base na rotação e velocidade do carro
                -MathF.Cos(Rotation) * _speed // Calcula a componente Y da velocidade com base na rotação e velocidade do carro
            );
            Position += _velocity * delta; // Atualiza a posição do carro com base na velocidade e no tempo decorrido
        }
        
        public void ApplyCollisionDamage(float amount)
        {
         Stats.CurrentDamage = MathHelper.Min(Stats.CurrentDamage + amount, 100f); // Aumenta o dano atual do carro, garantindo que não ultrapasse 100%
        }

        public void Repair()
        {
            Stats.CurrentDamage = 0f; // Restaura o dano do carro para zero, efetivamente reparando-o

        }

        private float getDamageSpeedMultiplier()
        {
            if(Stats.CurrentDamage >= 75f)return 0.4f; // Se o dano for 75% ou mais, a velocidade máxima é reduzida para 40% da velocidade original
            if(Stats.CurrentDamage >= 50f)return 0.7f; // Se o dano for 50% ou mais, a velocidade máxima é reduzida para 70% da velocidade original
            return 1f; // Se o dano for menor que 50%, a velocidade máxima é mantida

        }

        private Rectangle GetBounds()
        {
          return new Rectangle(
                (int)(Position.X - CarWidth / 2f), // Calcula a posição X do retângulo de colisão com base na posição do carro e na largura
                (int)(Position.Y - CarHeight / 2f), // Calcula a posição Y do retângulo de colisão com base na posição do carro e na altura
                CarWidth, // Define a largura do retângulo de colisão
                CarHeight // Define a altura do retângulo de colisão
            );
        }        

        public float CurrentSpeed => _speed; // Propriedade para acessar a velocidade atual do carro, útil para exibição ou lógica de jogo

        private float NormalizeAngle(float angle) // Normaliza o ângulo para manter a rotação dentro de um intervalo de -180 a 180 graus, facilitando o controle da rotação do carro
        {
            while (angle > MathHelper.Pi) angle -= MathHelper.TwoPi; // Se o ângulo for maior que 180 graus, subtraia 360 graus para trazê-lo de volta ao intervalo
            while (angle < -MathHelper.Pi) angle += MathHelper.TwoPi;  // Se o ângulo for menor que -180 graus, adicione 360 graus para trazê-lo de volta ao intervalo
            return angle; 
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture)
        {

            Vector2 origin = new Vector2(CarWidth / 2f, CarHeight / 2f); // Define o ponto de origem para a rotação do carro, centralizando-o


              spriteBatch.Draw(
                texture:     pixelTexture,
                position:    Position,                    // posição do centro
                sourceRectangle: new Rectangle(0, 0, 1, 1),
                color:       CarColor,
                rotation:    Rotation,
                origin:      new Vector2(0.5f, 0.5f),     // origem no centro do pixel 1x1
                scale:       new Vector2(CarWidth, CarHeight), // escala para o tamanho do carro
                effects:     SpriteEffects.None,
                layerDepth:  0f
                );

                Vector2 frontOffset = new Vector2( //
                    MathF.Sin(Rotation),
                    -MathF.Cos(Rotation)
                ) * (CarHeight / 2f + 2f); // Calcula o deslocamento para a frente do carro com base na rotação e na altura do carro
                

                spriteBatch.Draw(
                    texture:     pixelTexture,
                    position:    Position + frontOffset,
                    sourceRectangle: new Rectangle(0, 0, 1, 1),
                    color:       Color.Orange,
                    rotation:    0f,
                    origin:      new Vector2(0.5f, 0.5f),
                    scale:       new Vector2(8, 8),          // quadrado 8x8
                    effects:     SpriteEffects.None,
                    layerDepth:  0f
                    );

        }
    }
}