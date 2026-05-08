using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;
using System;

namespace BurnoutCity.Core
{
    /// <summary>
    /// Sistema central de áudio do BurnoutCity.
    /// Gere música de fundo, sons de efeitos e motor.
    /// Usa como singleton: AudioManager.Instance
    /// </summary>
    public class AudioManager
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        private static AudioManager? _instance;
        public static AudioManager Instance => _instance ??= new AudioManager();

        // ── Música ────────────────────────────────────────────────────────────
        private Song? _musicExploration;
        private Song? _musicRace;
        private string _currentMusic = "";

        // ── Efeitos de som ────────────────────────────────────────────────────
        private SoundEffect? _sfxCollision;
        private SoundEffect? _sfxGearChange;
        private SoundEffect? _sfxMenuClick;
        private SoundEffect? _sfxMenuHover;
        private SoundEffect? _sfxEngineLoop;
        private SoundEffect? _sfxNitroLoop;

        // Instâncias para sons em loop
        private SoundEffectInstance? _engineInstance;
        private SoundEffectInstance? _nitroInstance;

        // ── Volume ────────────────────────────────────────────────────────────
        public float MusicVolume { get; private set; } = 0.5f;
        public float SfxVolume   { get; private set; } = 0.8f;

        // ── Estado do motor ───────────────────────────────────────────────────
        private float _enginePitch    = 0f;   // -1.0 a 1.0
        private float _targetPitch    = 0f;
        private bool  _engineRunning  = false;

        private AudioManager() { }

        // ─────────────────────────────────────────────────────────────────────
        //  LOAD CONTENT
        // ─────────────────────────────────────────────────────────────────────
        public void LoadContent(ContentManager content)
        {
            try
            {
                _musicExploration = content.Load<Song>("Audio/music_exploration");
                _musicRace        = content.Load<Song>("Audio/music_race");

                _sfxCollision  = content.Load<SoundEffect>("Audio/collision");
                _sfxGearChange = content.Load<SoundEffect>("Audio/gear_change");
                _sfxMenuClick  = content.Load<SoundEffect>("Audio/menu_click");
                _sfxMenuHover  = content.Load<SoundEffect>("Audio/menu_hover");
                _sfxEngineLoop = content.Load<SoundEffect>("Audio/engine_loop");
                _sfxNitroLoop  = content.Load<SoundEffect>("Audio/nitro_loop");

                // Criar instâncias de loop
                _engineInstance = _sfxEngineLoop.CreateInstance();
                _engineInstance.IsLooped = true;
                _engineInstance.Volume   = SfxVolume * 0.7f;

                _nitroInstance = _sfxNitroLoop.CreateInstance();
                _nitroInstance.IsLooped = true;
                _nitroInstance.Volume   = SfxVolume * 0.6f;

                MediaPlayer.IsRepeating = true;
                MediaPlayer.Volume      = MusicVolume;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[AudioManager] Erro ao carregar áudio: {e.Message}");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  UPDATE  (chama no Update do jogo para suavizar pitch do motor)
        // ─────────────────────────────────────────────────────────────────────
        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Suavizar pitch do motor
            _enginePitch = MathHelper.Lerp(_enginePitch, _targetPitch, dt * 4f);
            if (_engineInstance != null && _engineRunning)
                _engineInstance.Pitch = MathHelper.Clamp(_enginePitch, -1f, 1f);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  MÚSICA
        // ─────────────────────────────────────────────────────────────────────
        public void PlayExplorationMusic()
        {
            if (_currentMusic == "exploration") return;
            _currentMusic = "exploration";
            if (_musicExploration != null)
            {
                MediaPlayer.Stop();
                MediaPlayer.Play(_musicExploration);
                MediaPlayer.Volume = MusicVolume;
            }
        }

        public void PlayRaceMusic()
        {
            if (_currentMusic == "race") return;
            _currentMusic = "race";
            if (_musicRace != null)
            {
                MediaPlayer.Stop();
                MediaPlayer.Play(_musicRace);
                MediaPlayer.Volume = MusicVolume;
            }
        }

        public void StopMusic()
        {
            _currentMusic = "";
            MediaPlayer.Stop();
        }

        public void PauseMusic()  => MediaPlayer.Pause();
        public void ResumeMusic() => MediaPlayer.Resume();

        // ─────────────────────────────────────────────────────────────────────
        //  MOTOR
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Chama no Update do ExplorationState/RaceState com a velocidade actual.
        /// speed: 0 a MaxSpeed
        /// </summary>
        public void UpdateEngine(float speed, float maxSpeed)
        {
            if (_engineInstance == null) return;

            if (!_engineRunning)
                StartEngine();

            // Pitch varia de -0.5 (parado) a 0.8 (velocidade máxima)
            float ratio   = MathHelper.Clamp(Math.Abs(speed) / maxSpeed, 0f, 1f);
            _targetPitch  = MathHelper.Lerp(-0.5f, 0.8f, ratio);

            // Volume sobe ligeiramente com velocidade
            _engineInstance.Volume = SfxVolume * MathHelper.Lerp(0.4f, 0.8f, ratio);
        }

        public void StartEngine()
        {
            _engineRunning = true;
            _engineInstance?.Play();
        }

        public void StopEngine()
        {
            _engineRunning = false;
            _engineInstance?.Stop();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  NITRO
        // ─────────────────────────────────────────────────────────────────────
        public void StartNitro()
        {
            if (_nitroInstance?.State != SoundState.Playing)
                _nitroInstance?.Play();
        }

        public void StopNitro()
        {
            _nitroInstance?.Stop();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  EFEITOS PONTUAIS
        // ─────────────────────────────────────────────────────────────────────
        public void PlayCollision(float intensity = 1.0f)
        {
            _sfxCollision?.Play(
                volume: SfxVolume * MathHelper.Clamp(intensity, 0.3f, 1.0f),
                pitch: 0f, pan: 0f);
        }

        public void PlayGearChange()
        {
            _sfxGearChange?.Play(volume: SfxVolume * 0.6f, pitch: 0f, pan: 0f);
        }

        public void PlayMenuClick()
        {
            _sfxMenuClick?.Play(volume: SfxVolume, pitch: 0f, pan: 0f);
        }

        public void PlayMenuHover()
        {
            _sfxMenuHover?.Play(volume: SfxVolume * 0.5f, pitch: 0f, pan: 0f);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  VOLUME
        // ─────────────────────────────────────────────────────────────────────
        public void SetMusicVolume(float volume)
        {
            MusicVolume        = MathHelper.Clamp(volume, 0f, 1f);
            MediaPlayer.Volume = MusicVolume;
        }

        public void SetSfxVolume(float volume)
        {
            SfxVolume = MathHelper.Clamp(volume, 0f, 1f);
            if (_engineInstance != null) _engineInstance.Volume = SfxVolume * 0.7f;
            if (_nitroInstance  != null) _nitroInstance.Volume  = SfxVolume * 0.6f;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  DISPOSE
        // ─────────────────────────────────────────────────────────────────────
        public void Dispose()
        {
            _engineInstance?.Stop();
            _nitroInstance?.Stop();
            _engineInstance?.Dispose();
            _nitroInstance?.Dispose();
            MediaPlayer.Stop();
        }
    }
}
