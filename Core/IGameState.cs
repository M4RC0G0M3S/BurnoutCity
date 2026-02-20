using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace BurnoutCity.Core
{
    public interface IGameState
    {
       void Initialize(GraphicsDevice graphicsDevice, ContentManager contentManager ); //define o estado inicial quando um estado é ativado

       void LoadContent(); //carrega tudo: texturas, sons e outros recursos

       void Update(GameTime gameTime); //Acontece toda a logica do estado ativo 

       void Draw(SpriteBatch spriteBatch); //exibe as texturas do estado carregado
       void UnloadContent(); //libera os recursos usados pelo estado ativo quando e desativado
    }
}



