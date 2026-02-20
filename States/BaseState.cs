using BurnoutCity.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace BurnoutCity.States
{
    
     public abstract class BaseState : IGameState
    {
        protected GraphicsDevice GraphicsDevice = null!; //propriedade para acessar o dispositivo gráfico, necessário para renderizar os gráficos do jogo        
        protected ContentManager ContentManager = null!; //propriedade para acessar o gerenciador de conteúdo, necessário para carregar os recursos do jogo, como texturas, sons e outros ativos

        public virtual void Initialize(GraphicsDevice graphicsDevice, ContentManager contentManager) //define o estado inicial quando um estado é ativado
        {
            GraphicsDevice = graphicsDevice;
            ContentManager = contentManager;
        }

        public abstract void LoadContent(); //carrega tudo: texturas, sons e outros recursos
        public abstract void Update(GameTime gameTime); //Acontece toda a logica do estado ativo
        public abstract void Draw(SpriteBatch spriteBatch); //exibe as texturas do estado carregado
        public virtual void UnloadContent() { } //libera os recursos usados pelo estado ativo quando e desativado, pode ser sobrescrito por estados que necessitem liberar recursos específicos
    }




}