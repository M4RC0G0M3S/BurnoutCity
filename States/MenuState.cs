using System;
using BurnoutCity.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BurnoutCity.States
{
    public class MenuState : BaseState
    {
        private SpriteFont _fontBig = null!;
        private SpriteFont _fontMedium = null!;
        private SpriteFont _fontSmall = null!;

        private KeyboardState _previousKeyboard;

        private int _selectedIndex;
        private bool _hasSave;
        private float _time;

        private MenuScreen _screen = MenuScreen.Main;

        private readonly string[] _mainOptions =
        {
            "CONTINUAR",
            "NOVO JOGO",
            "GUARDAR JOGO",
            "CARREGAR JOGO",
            "OPCOES AUDIO",
            "SAIR"
        };

        private readonly string[] _audioOptions =
        {
            "MUSICA",
            "EFEITOS",
            "VOLTAR"
        };

        private enum MenuScreen
        {
            Main,
            Audio,
            SaveSlots,
            LoadSlots,
            NewGameSlots
        }

        public override void LoadContent()
        {
            _fontBig = ContentManager.Load<SpriteFont>("Fonts/FontBig");
            _fontMedium = ContentManager.Load<SpriteFont>("Fonts/FontMedium");
            _fontSmall = ContentManager.Load<SpriteFont>("Fonts/FontSmall");

            _hasSave = SaveManager.Instance.HasAnySave();
            _selectedIndex = _hasSave ? 0 : 1;

            AudioManager.Instance.LoadContent(ContentManager);
            AudioManager.Instance.StopEngine();
            AudioManager.Instance.PlayExplorationMusic();
        }

        public override void Update(GameTime gameTime)
        {
            _time += (float)gameTime.ElapsedGameTime.TotalSeconds;
            _hasSave = SaveManager.Instance.HasAnySave();

            KeyboardState keyboard = Keyboard.GetState();

            if (WasPressed(keyboard, Keys.Down) || WasPressed(keyboard, Keys.S))
                MoveSelection(1);

            if (WasPressed(keyboard, Keys.Up) || WasPressed(keyboard, Keys.W))
                MoveSelection(-1);

            if (WasPressed(keyboard, Keys.Enter) || WasPressed(keyboard, Keys.Space))
                ActivateSelectedOption();

            if (WasPressed(keyboard, Keys.Left) || WasPressed(keyboard, Keys.A))
                ChangeVolume(-0.1f);

            if (WasPressed(keyboard, Keys.Right) || WasPressed(keyboard, Keys.D))
                ChangeVolume(0.1f);

            if (WasPressed(keyboard, Keys.Escape))
                GoBack();

            AudioManager.Instance.Update(gameTime);
            _previousKeyboard = keyboard;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Texture2D pixel = BurnoutCity.Game1.PixelTexture;

            DrawBackground(spriteBatch, pixel);
            DrawTitle(spriteBatch);

            switch (_screen)
            {
                case MenuScreen.Main:
                    DrawMainMenu(spriteBatch, pixel);
                    break;

                case MenuScreen.Audio:
                    DrawAudioMenu(spriteBatch, pixel);
                    break;

                case MenuScreen.SaveSlots:
                    DrawSlotMenu(spriteBatch, pixel, "GUARDAR JOGO", "Escolhe um slot para guardar o progresso atual.");
                    break;

                case MenuScreen.LoadSlots:
                    DrawSlotMenu(spriteBatch, pixel, "CARREGAR JOGO", "Escolhe um slot guardado para continuar.");
                    break;

                case MenuScreen.NewGameSlots:
                    DrawSlotMenu(spriteBatch, pixel, "NOVO JOGO", "Escolhe o slot onde queres criar uma nova partida.");
                    break;
            }

            DrawFooter(spriteBatch);
        }

        private void DrawBackground(SpriteBatch spriteBatch, Texture2D pixel)
        {
            spriteBatch.Draw(
                pixel,
                new Rectangle(0, 0, BurnoutCity.Game1.ScreenWidth, BurnoutCity.Game1.ScreenHeight),
                new Color(8, 8, 14)
            );

            for (int y = 0; y < BurnoutCity.Game1.ScreenHeight; y += 36)
            {
                float pulse = 0.10f + 0.05f * (float)Math.Sin(_time * 1.5f + y * 0.02f);

                spriteBatch.Draw(
                    pixel,
                    new Rectangle(0, y, BurnoutCity.Game1.ScreenWidth, 1),
                    Color.White * pulse
                );
            }

            for (int x = 0; x < BurnoutCity.Game1.ScreenWidth; x += 64)
            {
                spriteBatch.Draw(
                    pixel,
                    new Rectangle(x, 0, 1, BurnoutCity.Game1.ScreenHeight),
                    Color.OrangeRed * 0.05f
                );
            }

            spriteBatch.Draw(pixel, new Rectangle(0, 0, BurnoutCity.Game1.ScreenWidth, 6), Color.OrangeRed * 0.8f);
            spriteBatch.Draw(pixel, new Rectangle(0, BurnoutCity.Game1.ScreenHeight - 6, BurnoutCity.Game1.ScreenWidth, 6), Color.OrangeRed * 0.45f);

            DrawCitySilhouette(spriteBatch, pixel);
        }

        private void DrawCitySilhouette(SpriteBatch spriteBatch, Texture2D pixel)
        {
            int baseY = 560;

            for (int i = 0; i < 18; i++)
            {
                int width = 45 + (i % 4) * 15;
                int height = 80 + (i % 5) * 26;
                int x = i * 78 - 25;
                int y = baseY - height;

                spriteBatch.Draw(pixel, new Rectangle(x, y, width, height), Color.Black * 0.45f);

                for (int wy = y + 12; wy < baseY - 12; wy += 20)
                {
                    for (int wx = x + 8; wx < x + width - 8; wx += 16)
                    {
                        if ((wx + wy + i) % 3 == 0)
                            spriteBatch.Draw(pixel, new Rectangle(wx, wy, 5, 7), Color.OrangeRed * 0.20f);
                    }
                }
            }
        }

        private void DrawTitle(SpriteBatch spriteBatch)
        {
            string title = "BURNOUT CITY";

            float glow = 0.40f + 0.25f * (float)Math.Sin(_time * 2.2f);

            DrawCentered(spriteBatch, _fontBig, title, 71, Color.Black * 0.65f, new Vector2(5, 5));
            DrawCentered(spriteBatch, _fontBig, title, 68, Color.OrangeRed * glow, new Vector2(2, 2));
            DrawCentered(spriteBatch, _fontBig, title, 66, Color.OrangeRed);
            DrawCentered(spriteBatch, _fontSmall, "STREET RACING - GARAGE - RIVALS", 121, Color.LightGray);
        }

        private void DrawMainMenu(SpriteBatch spriteBatch, Texture2D pixel)
        {
            DrawCentered(spriteBatch, _fontSmall, "W/S ou SETAS para escolher | ENTER para confirmar", 153, Color.Gray);

            Rectangle panel = new Rectangle(
                BurnoutCity.Game1.ScreenWidth / 2 - 360,
                190,
                720,
                430
            );

            DrawPanel(spriteBatch, pixel, panel);

            for (int i = 0; i < _mainOptions.Length; i++)
            {
                bool disabled = (i == 0 && !_hasSave) || (i == 3 && !_hasSave);

                string text = _mainOptions[i];

                if (disabled)
                    text += "  / SEM SAVE";

                DrawButton(spriteBatch, pixel, text, i, disabled, 230, 58);
            }
        }

        private void DrawAudioMenu(SpriteBatch spriteBatch, Texture2D pixel)
        {
            DrawCentered(spriteBatch, _fontMedium, "OPCOES AUDIO", 150, Color.White);
            DrawCentered(spriteBatch, _fontSmall, "A/D ou SETAS ESQ/DIR para mudar volume", 185, Color.Gray);

            Rectangle panel = new Rectangle(
                BurnoutCity.Game1.ScreenWidth / 2 - 340,
                230,
                680,
                300
            );

            DrawPanel(spriteBatch, pixel, panel);

            DrawButton(spriteBatch, pixel, $"MUSICA   {(int)(AudioManager.Instance.MusicVolume * 100)}%", 0, false, 280, 70);
            DrawButton(spriteBatch, pixel, $"EFEITOS  {(int)(AudioManager.Instance.SfxVolume * 100)}%", 1, false, 280, 70);
            DrawButton(spriteBatch, pixel, "VOLTAR", 2, false, 280, 70);
        }

        private void DrawSlotMenu(SpriteBatch spriteBatch, Texture2D pixel, string title, string help)
        {
            DrawCentered(spriteBatch, _fontMedium, title, 150, Color.White);
            DrawCentered(spriteBatch, _fontSmall, help, 185, Color.Gray);

            Rectangle panel = new Rectangle(
                BurnoutCity.Game1.ScreenWidth / 2 - 380,
                230,
                760,
                320
            );

            DrawPanel(spriteBatch, pixel, panel);

            for (int i = 0; i < SaveManager.SlotCount; i++)
            {
                int slot = i + 1;
                bool disabled = _screen == MenuScreen.LoadSlots && !SaveManager.Instance.HasSave(slot);

                string text = SaveManager.Instance.GetSlotLabel(slot);

                DrawButton(spriteBatch, pixel, text, i, disabled, 285, 75);
            }

            DrawCentered(spriteBatch, _fontSmall, "Guardar ou Novo Jogo substitui o slot escolhido.", 585, Color.DarkGray);
        }

        private void DrawPanel(SpriteBatch spriteBatch, Texture2D pixel, Rectangle panel)
        {
            spriteBatch.Draw(pixel, new Rectangle(panel.X + 14, panel.Y + 14, panel.Width, panel.Height), Color.Black * 0.50f);
            spriteBatch.Draw(pixel, panel, new Color(22, 22, 32) * 0.96f);

            spriteBatch.Draw(pixel, new Rectangle(panel.X, panel.Y, 4, panel.Height), Color.OrangeRed);
            spriteBatch.Draw(pixel, new Rectangle(panel.X, panel.Y, panel.Width, 2), Color.White * 0.12f);
            spriteBatch.Draw(pixel, new Rectangle(panel.X, panel.Bottom - 2, panel.Width, 2), Color.Black * 0.5f);
        }

        private void DrawButton(SpriteBatch spriteBatch, Texture2D pixel, string text, int index, bool disabled, int startY, int spacing)
        {
            bool selected = index == _selectedIndex;

            Rectangle button = new Rectangle(
                BurnoutCity.Game1.ScreenWidth / 2 - 270,
                startY + index * spacing,
                540,
                46
            );

            Color baseColor = selected && !disabled
                ? new Color(255, 95, 18)
                : new Color(48, 48, 62);

            Color textColor = disabled
                ? Color.DarkGray
                : selected
                    ? Color.White
                    : new Color(220, 220, 225);

            spriteBatch.Draw(pixel, new Rectangle(button.X + 6, button.Y + 6, button.Width, button.Height), Color.Black * 0.30f);
            spriteBatch.Draw(pixel, button, baseColor);

            if (selected && !disabled)
            {
                float pulse = 0.55f + 0.35f * (float)Math.Sin(_time * 6f);

                spriteBatch.Draw(pixel, new Rectangle(button.X - 8, button.Y, 6, button.Height), Color.Yellow * pulse);
                spriteBatch.Draw(pixel, new Rectangle(button.X, button.Y, button.Width, 2), Color.Yellow);
                spriteBatch.Draw(pixel, new Rectangle(button.X, button.Bottom - 2, button.Width, 2), Color.Yellow * 0.8f);
                spriteBatch.Draw(pixel, new Rectangle(button.X, button.Y, button.Width, button.Height / 2), Color.White * 0.08f);
            }
            else
            {
                spriteBatch.Draw(pixel, new Rectangle(button.X, button.Y, button.Width, 1), Color.White * 0.08f);
            }

            Vector2 size = _fontMedium.MeasureString(text);

            spriteBatch.DrawString(
                _fontMedium,
                text,
                new Vector2(button.X + (button.Width - size.X) / 2f, button.Y + (button.Height - size.Y) / 2f),
                textColor
            );
        }

        private void DrawFooter(SpriteBatch spriteBatch)
        {
            DrawCentered(spriteBatch, _fontSmall, "ESC para voltar - Saves em slots - Burnout City", 665, Color.DarkGray);
        }

        private void ActivateSelectedOption()
        {
            switch (_screen)
            {
                case MenuScreen.Main:
                    ActivateMainOption();
                    break;

                case MenuScreen.Audio:
                    ActivateAudioOption();
                    break;

                case MenuScreen.SaveSlots:
                    SaveToSelectedSlot();
                    break;

                case MenuScreen.LoadSlots:
                    LoadSelectedSlot();
                    break;

                case MenuScreen.NewGameSlots:
                    NewGameInSelectedSlot();
                    break;
            }
        }

        private void ActivateMainOption()
        {
            if (_selectedIndex == 0 && _hasSave)
            {
                _screen = MenuScreen.LoadSlots;
                _selectedIndex = FirstAvailableSlotIndex();
                AudioManager.Instance.PlayMenuClick();
            }
            else if (_selectedIndex == 1)
            {
                _screen = MenuScreen.NewGameSlots;
                _selectedIndex = 0;
                AudioManager.Instance.PlayMenuClick();
            }
            else if (_selectedIndex == 2)
            {
                _screen = MenuScreen.SaveSlots;
                _selectedIndex = 0;
                AudioManager.Instance.PlayMenuClick();
            }
            else if (_selectedIndex == 3 && _hasSave)
            {
                _screen = MenuScreen.LoadSlots;
                _selectedIndex = FirstAvailableSlotIndex();
                AudioManager.Instance.PlayMenuClick();
            }
            else if (_selectedIndex == 4)
            {
                _screen = MenuScreen.Audio;
                _selectedIndex = 0;
                AudioManager.Instance.PlayMenuClick();
            }
            else if (_selectedIndex == 5)
            {
                BurnoutCity.Game1.QuitGame?.Invoke();
            }
        }

        private void ActivateAudioOption()
        {
            if (_selectedIndex == 2)
            {
                _screen = MenuScreen.Main;
                _selectedIndex = 4;
                AudioManager.Instance.PlayMenuClick();
            }
        }

        private void SaveToSelectedSlot()
        {
            int slot = _selectedIndex + 1;

            SaveManager.Instance.SaveFromPlayerDataToSlot(
                GameStateManager.Instance.PlayerData,
                slot
            );

            _hasSave = SaveManager.Instance.HasAnySave();
            _screen = MenuScreen.Main;
            _selectedIndex = 0;

            AudioManager.Instance.PlayMenuClick();
        }

        private void LoadSelectedSlot()
        {
            int slot = _selectedIndex + 1;

            if (!SaveManager.Instance.HasSave(slot))
                return;

            SaveManager.Instance.LoadSlot(slot);

            GameStateManager.Instance.PlayerData.LoadFrom(
                SaveManager.Instance.CurrentSave
            );

            AudioManager.Instance.StopEngine();
            GameStateManager.Instance.ChangeState(new ExplorationState());
        }

        private void NewGameInSelectedSlot()
        {
            int slot = _selectedIndex + 1;

            SaveManager.Instance.NewGameSlot(slot);

            GameStateManager.Instance.PlayerData.LoadFrom(
                SaveManager.Instance.CurrentSave
            );

            AudioManager.Instance.StopEngine();
            GameStateManager.Instance.ChangeState(new ExplorationState());
        }

        private void MoveSelection(int direction)
        {
            int length = GetCurrentOptionCount();
            int attempts = 0;

            do
            {
                _selectedIndex += direction;

                if (_selectedIndex < 0)
                    _selectedIndex = length - 1;

                if (_selectedIndex >= length)
                    _selectedIndex = 0;

                attempts++;

                if (attempts > length)
                    break;

            } while (IsCurrentOptionDisabled());

            AudioManager.Instance.PlayMenuHover();
        }

        private bool IsCurrentOptionDisabled()
        {
            if (_screen == MenuScreen.Main)
                return (_selectedIndex == 0 && !_hasSave) || (_selectedIndex == 3 && !_hasSave);

            if (_screen == MenuScreen.LoadSlots)
                return !SaveManager.Instance.HasSave(_selectedIndex + 1);

            return false;
        }

        private int GetCurrentOptionCount()
        {
            if (_screen == MenuScreen.Main)
                return _mainOptions.Length;

            if (_screen == MenuScreen.Audio)
                return _audioOptions.Length;

            return SaveManager.SlotCount;
        }

        private int FirstAvailableSlotIndex()
        {
            for (int i = 0; i < SaveManager.SlotCount; i++)
            {
                if (SaveManager.Instance.HasSave(i + 1))
                    return i;
            }

            return 0;
        }

        private void ChangeVolume(float amount)
        {
            if (_screen != MenuScreen.Audio)
                return;

            if (_selectedIndex == 0)
            {
                AudioManager.Instance.SetMusicVolume(AudioManager.Instance.MusicVolume + amount);
                AudioManager.Instance.PlayMenuHover();
            }
            else if (_selectedIndex == 1)
            {
                AudioManager.Instance.SetSfxVolume(AudioManager.Instance.SfxVolume + amount);
                AudioManager.Instance.PlayMenuHover();
            }
        }

        private void GoBack()
        {
            if (_screen == MenuScreen.Main)
                return;

            if (_screen == MenuScreen.Audio)
                _selectedIndex = 4;
            else if (_screen == MenuScreen.SaveSlots)
                _selectedIndex = 2;
            else if (_screen == MenuScreen.LoadSlots)
                _selectedIndex = 3;
            else if (_screen == MenuScreen.NewGameSlots)
                _selectedIndex = 1;

            _screen = MenuScreen.Main;
            AudioManager.Instance.PlayMenuClick();
        }

        private bool WasPressed(KeyboardState keyboard, Keys key)
        {
            return keyboard.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);
        }

        private void DrawCentered(SpriteBatch spriteBatch, SpriteFont font, string text, float y, Color color)
        {
            DrawCentered(spriteBatch, font, text, y, color, Vector2.Zero);
        }

        private void DrawCentered(SpriteBatch spriteBatch, SpriteFont font, string text, float y, Color color, Vector2 offset)
        {
            Vector2 size = font.MeasureString(text);
            Vector2 position = new Vector2((BurnoutCity.Game1.ScreenWidth - size.X) / 2f, y);
            position += offset;

            spriteBatch.DrawString(font, text, position, color);
        }
    }
}
