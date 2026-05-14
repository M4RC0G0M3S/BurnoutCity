# BurnoutCity

**Motor:** MonoGame (C#)  
**Perspetiva:** 2D Top-Down  
**Género:** Open World / Racing / Management / Simulation  

---

## Identificação do Projeto

| Campo | Informação |
|---|---|
| Cadeira | Técnicas de Desenvolvimento de Videojogos |
| Motor | MonoGame Framework (C#) |
| Plataforma | Windows / Linux / macOS |
| Resolução | 1280 × 720 px |

## Equipa

| Nome | Número de Aluno |
|---|---|
| Afonso Miranda | 27944 |
| Marco Gomes | 28550 |
| David Costa | 24609 |

---

## Conceito

BurnoutCity é um jogo de corridas urbanas em mundo aberto onde **o jogador É o carro** — não existe personagem humana. O carro é o protagonista que evolui ao longo do jogo. O jogador conduz livremente pela cidade, visita lojas para comprar upgrades, desafia rivais em corridas drag ao estilo CSR e progride do nível 1 até ao nível 20, com o objetivo de derrotar o campeão não oficial da cidade.

---

## Como Correr o Projeto

```
dotnet run
```

Requer .NET 8+ e o pacote MonoGame instalado. O ficheiro de save é criado automaticamente em `Saves/burnoutcity_save.json` na primeira execução.

---

## Estrutura de Pastas

```
BurnoutCity/
├── Core/
│   ├── GameStateManager.cs   — Máquina de estados principal
│   ├── Camera.cs             — Câmara com seguimento e clamping
│   ├── SaveManager.cs        — Serialização JSON do save
│   └── AudioManager.cs       — Gestão de sons e música
├── States/
│   ├── ExplorationState.cs   — Mundo aberto, condução livre
│   ├── RaceState.cs          — Corrida drag estilo CSR
│   ├── ShopState.cs          — Loja de performance
│   ├── GarageState.cs        — Reparação do carro
│   ├── TestTrackState.cs     — Pista de testes com cronómetro
│   ├── CustomizationState.cs — Personalização visual
│   ├── HighScoreState.cs     — Tabela de recordes
│   ├── CreditsState.cs       — Créditos
│   └── MenuState.cs          — Menu principal
├── Entities/
│   ├── Car.cs                — Carro do jogador (física, colisões, animações)
│   ├── CarStats.cs           — Estatísticas do carro
│   ├── Rival.cs              — Estrutura de dados de um rival
│   └── TrafficCar.cs         — Carro de tráfego autónomo
├── Map/
│   ├── MapManager.cs         — Carregamento de texturas do mapa
│   ├── BuildingManager.cs    — Posicionamento e colisão de edifícios
│   ├── Building.cs           — Entidade de edifício individual
│   ├── TriggerZone.cs        — Zona de trigger retangular
│   └── TriggerZoneManager.cs — Gestão de todas as zonas de trigger
├── UI/
│   └── HUD.cs                — HUD de exploração (velocidade, nível, dinheiro)
└── Data/
    ├── PlayerData.cs         — Estado persistente do jogador (XP, nível, upgrades)
    ├── SaveData.cs           — Estrutura serializada em JSON
    ├── RivalData.cs          — Lista estática de todos os rivais
    └── UpgradeData.cs        — Dados dos upgrades disponíveis
```

---

## GameStateManager

O `GameStateManager` é o núcleo do jogo. Implementa o padrão **Singleton** e gere qual estado (`IGameState`) está ativo a cada momento. Só um estado está ativo de cada vez; ao mudar de estado, o anterior liberta os seus recursos (`UnloadContent`) e o novo é inicializado e carrega o seu conteúdo.

```csharp
// Core/GameStateManager.cs

public void ChangeState(IGameState newState)
{
    _pendingState = newState; // define o próximo estado do jogo
}

private void AplyStateChange(IGameState newState)
{
    _currentState?.UnloadContent(); // liberta os recursos do estado atual
    _currentState = newState;
    _currentState.Initialize(_graphicsDevice, _contentManager);
    _currentState.LoadContent();
}
```

A mudança não é imediata — é colocada em `_pendingState` e aplicada no início do próximo `Update`, evitando problemas de concorrência no mesmo frame. Os estados disponíveis são:

| Estado | Descrição |
|---|---|
| `ExplorationState` | Mundo aberto — condução livre |
| `RaceState` | Corrida drag contra um rival |
| `ShopState` | Loja de upgrades de performance |
| `GarageState` | Reparação do carro |
| `TestTrackState` | Pista de testes com cronómetro |
| `CustomizationState` | Personalização visual do carro |
| `HighScoreState` | Tabela de recordes |
| `CreditsState` | Créditos da equipa |
| `MenuState` | Menu principal |

---

## O Jogador — Car.cs

O jogador **é o carro**. Não existe personagem humana. O carro é controlado com **WASD** e tem física de aceleração e atrito — não para nem arranca instantaneamente.

### Controlos

| Tecla | Ação |
|---|---|
| `W` | Acelerar |
| `S` | Travar / marcha-atrás |
| `A` | Virar à esquerda |
| `D` | Virar à direita |
| `E` | Entrar numa localização (loja, garagem, etc.) |
| `Space` | Mudar de mudança (durante corridas) |
| `N` | Ativar nitro (durante corridas) |

### Física

O carro usa um modelo simples mas eficaz. A velocidade acumula-se com a aceleração e é reduzida pelo atrito quando o jogador larga o acelerador:

```csharp
// Entities/Car.cs

private const float Friction    = 0.88f;  // atrito quando sem aceleração
private const float TurnSpeed   = 2.8f;   // velocidade de rotação
private const float MinSpeedToTurn = 10f; // velocidade mínima para virar

private void ApplyFriction()
{
    if (!_isAccelerating)
    {
        _speed *= Friction; // reduz progressivamente a velocidade
        if (Math.Abs(_speed) < 0.5f)
            _speed = 0f;    // para completamente abaixo do limiar
    }
}

private void ApplyMovement(float delta)
{
    _velocity = new Vector2(
        MathF.Sin(Rotation) * _speed,
       -MathF.Cos(Rotation) * _speed
    );
    Position += _velocity * delta;
}
```

O movimento é vetorial: a direção é calculada com seno/cosseno da rotação do carro, permitindo movimento suave em qualquer ângulo. O carro só vira se estiver acima de `MinSpeedToTurn`, o que evita rotação no lugar.

### Sistema de Dano

O carro tem um valor de dano entre 0% e 100%. Colisões com edifícios e tráfego aumentam o dano. Dano elevado reduz a velocidade máxima:

```csharp
// Entities/Car.cs

private float getDamageSpeedMultiplier()
{
    if (Stats.CurrentDamage >= 75f) return 0.4f; // 40% da velocidade máxima
    if (Stats.CurrentDamage >= 50f) return 0.7f; // 70% da velocidade máxima
    return 1f;                                    // sem penalidade
}
```

O carro é reparado na **Garagem** por um custo em dinheiro proporcional ao dano.

### CarStats

As estatísticas do carro são definidas em `CarStats.cs` e modificadas pelos upgrades:

```csharp
// Entities/CarStats.cs

public float MaxSpeed     { get; set; } = 300f;   // velocidade máxima (px/s)
public float Acceleration { get; set; } = 200f;   // aceleração (px/s²)
public float Handling     { get; set; } = 1.0f;   // multiplicador de viragem
public float NitroBoost   { get; set; } = 150f;   // boost adicional do nitro
public float CurrentDamage{ get; set; } = 0f;     // dano atual (0–100%)
```

---

## Câmara — Camera.cs

A câmara segue o carro do jogador com **interpolação linear (Lerp)** e nunca sai dos limites do mundo. O fator de suavização (`SmoothingFactor = 0.12f`) cria um efeito de câmara "flutuante" que segue o carro com um ligeiro atraso.

```csharp
// Core/Camera.cs

public void Update(Vector2 targetPosition)
{
    Target   = targetPosition;
    Position = Vector2.Lerp(Position, Target, SmoothingFactor);
    Position = ClampToWorldBounds(Position);
}

public Matrix GetTransform()
{
    return Matrix.CreateTranslation(
        -Position.X + _viewportWidth  / 2f,
        -Position.Y + _viewportHeight / 2f,
        0f
    );
}
```

A matriz de transformação é passada ao `SpriteBatch.Begin()`, deslocando tudo o que é desenhado para simular o movimento da câmara. O clamping garante que os limites do mundo nunca ficam visíveis:

```csharp
private Vector2 ClampToWorldBounds(Vector2 position)
{
    float halfW = _viewportWidth  / 2f;
    float halfH = _viewportHeight / 2f;

    return new Vector2(
        MathHelper.Clamp(position.X, WorldBounds.Left + halfW, WorldBounds.Right  - halfW),
        MathHelper.Clamp(position.Y, WorldBounds.Top  + halfH, WorldBounds.Bottom - halfH)
    );
}
```

---

## O Mundo — Mapa

O mundo **não é baseado em tiles**. Todos os edifícios, estradas e decorações são sprites posicionados manualmente com coordenadas absolutas. O mundo tem **3× o tamanho da janela** (3840 × 2160 px para uma janela de 1280 × 720).

### Edifícios e Colisões

Cada edifício é uma instância de `Building` com posição, tipo, sprite e um `Rectangle` de colisão (Bounds). O `BuildingManager` posiciona todos os edifícios estaticamente:

```csharp
// Map/Building.cs

public Building(Vector2 position, BuildingType type, MapManager mapManager, int size = 256)
{
    Position     = position;
    Type         = type;
    IsInteractive = type is BuildingType.PartsShop
                          or BuildingType.CustomShop
                          or BuildingType.Garage
                          or BuildingType.RacePoint
                          or BuildingType.TestTrack;

    _sprite = mapManager.GetTexture(_textureKeys[type])!;
    Bounds  = new Rectangle((int)position.X, (int)position.Y, size, size);
}
```

Edifícios interativos mostram uma borda laranja quando o jogador está a menos de 200px, a indicar que pode carregar `E` para entrar.

### Colisão AABB

A deteção de colisão entre o carro e os edifícios usa **AABB (Axis-Aligned Bounding Box)**. Quando há interseção, o eixo com menor sobreposição é usado para empurrar o carro para fora:

```csharp
// States/ExplorationState.cs

float overlapLeft   = carBounds.Right  - bb.Left;
float overlapRight  = bb.Right  - carBounds.Left;
float overlapTop    = carBounds.Bottom - bb.Top;
float overlapBottom = bb.Bottom - carBounds.Top;

bool  fromLeft = overlapLeft  < overlapRight;
bool  fromTop  = overlapTop   < overlapBottom;
float minX = fromLeft ? overlapLeft  : overlapRight;
float minY = fromTop  ? overlapTop   : overlapBottom;

Vector2 corrected = _playerCar.Position;
if (minX < minY)
    corrected.X += fromLeft ? -overlapLeft : overlapRight;
else
    corrected.Y += fromTop  ? -overlapTop  : overlapBottom;

_playerCar.SetPosition(corrected);
_playerCar.ApplyCollisionDamage(5f);
```

O mesmo algoritmo é usado para colisões com os limites do mundo e com o tráfego.

### Tipos de Edifícios

```
PartsShop        — Loja de upgrades de performance
CustomShop       — Loja de personalização visual
Garage           — Reparação do carro
RacePoint        — Ponto de corrida (inicia corridas drag)
TestTrack        — Pista de testes com cronómetro
Hotel            — Decorativo
Restaurante      — Decorativo
Predio           — Decorativo
PredioResidencias— Decorativo
Residencias      — Decorativo
PredioIndustrias — Decorativo
```

---

## Zonas de Trigger — TriggerZone

Cada localização interativa tem uma **zona de trigger** — um retângulo invisível à frente da porta. Quando o jogador entra na zona e carrega `E`, o `TriggerZoneManager` invoca o evento `OnZoneEntered`, que muda o estado do jogo:

```csharp
// Map/TriggerZoneManager.cs

private void PlaceAllZones()
{
    AddZone(704,  1600, TriggerZoneType.Garage,    offsetX: 28, offsetY: -80);
    AddZone(704,  832,  TriggerZoneType.PartsShop, offsetX: 28, offsetY: -80);
    AddZone(2240, 832,  TriggerZoneType.CustomShop,offsetX: 28, offsetY: -80);
    AddZone(2624, 1600, TriggerZoneType.RacePoint, offsetX: 28, offsetY: -80);
    AddZone(1088, 320,  TriggerZoneType.TestTrack, offsetX: 28, offsetY: -80);
}
```

A deteção é feita por `IsTriggeredBy`, que verifica se o centro do carro está dentro dos bounds da zona:

```csharp
// Map/TriggerZone.cs

public bool IsTriggeredBy(Vector2 playerCenter)
{
    if (!IsActive) return false;
    return Bounds.Contains((int)playerCenter.X, (int)playerCenter.Y);
}
```

---

## Corrida Drag — RaceState

As corridas são do tipo **drag race em linha reta** ao estilo CSR. Não há steering durante a corrida — o jogador gere apenas as mudanças e o nitro.

### Fases da Corrida

```
Preview     → Card do rival mostrado (2.5s)
Countdown   → Semáforo: vermelho × 3 → verde
Racing      → Corrida em andamento
Finished    → Resultado (vitória/derrota, XP, dinheiro)
```

### Marchas

Existem **6 mudanças**. O jogador carrega `Space` para mudar. Uma **barra de rotações** (rev bar) indica o momento ideal para mudar:

```csharp
// States/RaceState.cs

private void ProcessGearShift(float ratio)
{
    // ratio: posição atual na barra (0.0 a 1.0)
    if (ratio >= 0.55f && ratio <= 0.80f)
    {
        _playerSpeed *= 1.08f;          // +8% — PERFECT!
        _shiftFeedbackText = "PERFECT!";
        _shiftFeedbackColor = new Color(50, 255, 80);
    }
    else if ((ratio >= 0.40f && ratio < 0.55f) || (ratio > 0.80f && ratio <= 0.92f))
    {
        _shiftFeedbackText = "GOOD";    // sem bónus nem penalização
        _shiftFeedbackColor = new Color(255, 200, 50);
    }
    else
    {
        _playerSpeed *= 0.82f;          // -18% — MISS!
        _shiftFeedbackText = "MISS!";
        _shiftFeedbackColor = new Color(255, 60, 60);
    }

    _currentGear++;
    _revTimer = 0f;
}
```

| Zona da barra | Resultado | Efeito na velocidade |
|---|---|---|
| Verde (55%–80%) | PERFECT | +8% |
| Amarelo (40%–55% ou 80%–92%) | GOOD | sem alteração |
| Vermelho (fora das zonas) | MISS | −18% |

### Nitro

Carregar `N` ativa o nitro, multiplicando a velocidade efetiva por `1.55×`. A barra de nitro esgota-se enquanto ativo e recarrega lentamente quando inativo:

```csharp
_nitroActive = n && _nitroCharge > 0f;
_nitroCharge = _nitroActive
    ? (float)Math.Max(0f, _nitroCharge - NitroDrainRate * dt)    // -40/s
    : (float)Math.Min(NitroMaxCharge, _nitroCharge + NitroRechargeRate * dt); // +12/s
```

### IA do Rival

O rival segue uma curva de aceleração simples: sobe de mudança a cada 1.5 segundos e tem um ruído aleatório pequeno para parecer mais natural. A sua velocidade é escalada pela proporção entre o `MaxSpeed` do rival e o top speed base do jogador (480 px/s), garantindo que os upgrades do jogador fazem diferença:

```csharp
// States/RaceState.cs

float top = GearTopSpeeds[Math.Min(_rivalGear, MaxGears)]
            * (_rivalMaxSpeed / 480f)   // escala pelo nível do rival
            + _rivalNoise;              // pequena variação aleatória

_rivalSpeed = _rivalSpeed < top
    ? (float)Math.Min(_rivalSpeed + _rivalAccel * dt, top)
    : MathHelper.Lerp(_rivalSpeed, top, 0.1f);
```

### Recompensas

```
Vitória:  +200 XP  | +2000 €
Derrota:  +20 XP   | +200 €
```

Perder uma corrida **nunca remove dinheiro**. O jogador recebe sempre uma recompensa menor.

---

## Rivais — RivalData.cs

Existem **6 rivais** mais o campeão final. Cada rival tem um ID único, nome, carro, cor, velocidade máxima, citação pré-corrida e recompensa adicional pela primeira derrota.

```csharp
// Data/RivalData.cs

new Rival {
    Id           = "rival_01",
    Name         = "Rusty",
    CarName      = "Civic Enferrujado",
    CarColor     = new Color(160, 80, 50),
    MaxSpeed     = 500f,
    Acceleration = 500f,
    BonusReward  = 500,
    PreRaceQuote = "Vai buscar as rodas de treino, novato."
},
// ...
new Rival {
    Id           = "rival_champion",
    Name         = "The King",
    CarName      = "Darkstar GT",
    CarColor     = new Color(255, 140, 0),
    MaxSpeed     = 2000f,
    Acceleration = 2000f,
    BonusReward  = 15000,
    PreRaceQuote = "Toda a gente tenta. Ninguém passa."
},
```

A progressão é sequencial — o jogador enfrenta o primeiro rival não derrotado da lista. Rivais derrotados ficam registados em `PlayerData.DefeatedRivals` (lista de IDs).

| Rival | Carro | Velocidade Base |
|---|---|---|
| Rusty | Civic Enferrujado | 500 px/s |
| Spike | Golf Turbinado | 700 px/s |
| Nova | Supra Clone | 900 px/s |
| Phantom | R34 das Sombras | 1100 px/s |
| Nitro King | EVO Azul-Fogo | 1500 px/s |
| Blaze | NSX Fantasma | 1800 px/s |
| **The King** | **Darkstar GT** | **2000 px/s** |

---

## Progressão — PlayerData.cs

O jogador progride do **nível 1 ao 20** através de XP acumulado em corridas. A tabela de XP usa a fórmula `300 × nível^1.35` arredondada a 50, criando uma curva gradual:

```csharp
// Data/PlayerData.cs

private static int[] BuildXpTable()
{
    var table = new int[19]; // 19 transições: nível 1→2 até 19→20
    for (int i = 0; i < 19; i++)
    {
        int lvl = i + 1;
        double raw = 300.0 * Math.Pow(lvl, 1.35);
        table[i] = (int)(Math.Round(raw / 50.0) * 50);
    }
    return table;
}
```

### Registo de Resultado de Corrida

```csharp
// Data/PlayerData.cs

public LevelUpInfo RegisterRaceResult(bool won, string? rivalId = null)
{
    int xpGained    = won ? XP_WIN    : XP_LOSS;    // 200 ou 20 XP
    int moneyGained = won ? MONEY_WIN : MONEY_LOSS;  // 2000 ou 200 €

    Money += moneyGained; // dinheiro NUNCA é removido por derrota

    if (won)
    {
        TotalWins++;
        if (rivalId != null && !DefeatedRivals.Contains(rivalId))
            DefeatedRivals.Add(rivalId); // marca rival como derrotado
    }
    else TotalLosses++;

    return AddXP(xpGained); // retorna LevelUpInfo (subiu de nível?)
}
```

---

## Upgrades — ShopState

A loja de performance tem 4 categorias de upgrade, cada uma com **4 tiers** (0 = stock, 4 = máximo). Cada tier tem um custo em dinheiro e um requisito mínimo de nível:

```csharp
// States/ShopState.cs

private readonly int[] _costs = { 0, 2000, 5000, 12000, 25000 };
private readonly int[] _reqs  = { 1, 3,    7,    12,    18    };
```

| Upgrade | Efeito |
|---|---|
| Motor (Engine) | Aumenta velocidade máxima |
| Pneus (Tires) | Aumenta handling |
| Turbo | Aumenta aceleração |
| Nitro | Aumenta capacidade e poder do nitro |

Upgrades bloqueados (nível insuficiente) são visíveis mas não compráveis. Cada compra faz auto-save automaticamente.

---

## Garagem — GarageState

A garagem permite reparar o carro. O custo é proporcional ao dano: `custo = dano × 10`. Uma barra visual mostra o estado atual do carro (verde = intacto, vermelho = danificado):

```csharp
// States/GarageState.cs

int repairCost = (int)(pd.CarDamage * 10);

if (_btnRepair.Contains(_currentMouse.Position))
{
    if (pd.CarDamage > 0 && pd.Money >= repairCost)
    {
        pd.SpendMoney(repairCost);
        pd.RepairCar(); // CurrentDamage = 0f
    }
}
```

---

## Pista de Testes — TestTrackState

A pista de testes usa exatamente a mesma física de mudanças e nitro que as corridas reais, mas **sem rival e sem recompensas**. Serve para o jogador aprender a mecânica de mudanças.

O cronómetro inicia automaticamente quando o jogador carrega em `W` e para ao cruzar a linha de chegada. Os **5 melhores tempos** são guardados no save:

```csharp
// States/TestTrackState.cs

// Fases: Waiting → Running → Finished
private enum RunState { Waiting, Running, Finished }

private void UpdateRunning(float dt, KeyboardState kb)
{
    _lapTime += dt; // cronómetro em andamento

    // (física igual ao RaceState: mudanças, nitro, aceleração)

    if (_progress >= 1f) FinishRun(); // chegou ao fim
}

private void FinishRun()
{
    _runState = RunState.Finished;
    _lastTime = _lapTime;
    _isRecord = _playerData.RegisterLapTime(_lapTime); // regista no top 5
}
```

Após terminar, o jogador pode carregar `R` para nova tentativa ou `Space/Enter` para voltar ao mundo aberto.

### Avaliação de Mudanças (igual à corrida)

```
Ratio 55%–80%  → PERFECT  (+8% velocidade)
Ratio 40%–55%  → GOOD     (sem alteração)
Resto          → MISS     (−18% velocidade)
```

---

## Tráfego — TrafficCar

Carros autónomos circulam pelas ruas durante a exploração. Cada `TrafficCar` segue uma lista de **waypoints** predefinidos, rodando suavemente em direção ao próximo ponto. O tráfego desaparece completamente durante as corridas.

Quando colide com o jogador, o carro de tráfego imobiliza-se durante um momento (simula o impacto) e o carro do jogador recebe dano. O tráfego nunca colide com outros carros de tráfego — apenas com o jogador.

---

## Sistema de Save — SaveManager

O save é automático após cada corrida e cada compra. Usa **escrita atómica** para proteger contra corrupção:

```csharp
// Core/SaveManager.cs

public void Save()
{
    string tempPath = SaveFilePath + ".tmp";
    string json = JsonSerializer.Serialize(CurrentSave, _jsonOptions);
    File.WriteAllText(tempPath, json);                          // 1. escreve no .tmp

    if (File.Exists(SaveFilePath))
        File.Copy(SaveFilePath, BackupFilePath, overwrite: true); // 2. faz backup

    File.Move(tempPath, SaveFilePath, overwrite: true);         // 3. substitui o principal
}
```

Se o ficheiro principal estiver corrompido, o `SaveManager` tenta carregar o `.bak`. Se ambos falharem, cria um save limpo. A validade do save é verificada pelo campo `Level` (deve estar entre 1 e 20).

### Dados Guardados

| Campo | Descrição |
|---|---|
| Level, XP | Progresso do jogador |
| Money | Saldo atual |
| TotalWins / TotalLosses | Histórico de corridas |
| DefeatedRivals | Lista de IDs de rivais derrotados |
| EngineLevel / TiresLevel / TurboLevel / NitroLevel | Upgrades instalados (0–4) |
| CarColorIndex | Cor do carro selecionada |
| WorldPositionX / WorldPositionY | Posição no mundo ao guardar |
| CarDamage | Estado de dano do carro (0–100%) |
| BestLapTimes | Top 5 tempos na pista de testes |

---

## Sistema de Som — AudioManager

O `AudioManager` é um Singleton que gere todos os sons e músicas do jogo. O som do motor varia o **pitch** com a velocidade atual:

```csharp
// Core/AudioManager.cs

public void UpdateEngine(float speed, float maxSpeed)
{
    float ratio  = MathHelper.Clamp(Math.Abs(speed) / maxSpeed, 0f, 1f);
    _targetPitch = MathHelper.Lerp(-0.5f, 0.8f, ratio); // pitch varia com velocidade
    _engineInstance.Volume = SfxVolume * MathHelper.Lerp(0.4f, 0.8f, ratio);
}
```

A música muda automaticamente com o estado do jogo: música de exploração no mundo aberto, música intensa durante as corridas.

### Sons Presentes

| Som | Trigger |
|---|---|
| Motor (loop) | Sempre ativo na exploração |
| Mudança | Ao carregar Space durante corrida |
| Nitro (whoosh) | Ao ativar nitro |
| Colisão (impacto) | Ao bater num edifício ou tráfego |
| Clique de menu | Ao clicar botões |
| Subida de nível | Ao subir de nível |

---

## Tabela de Recordes — HighScoreState

Mostra os **5 melhores tempos** na pista de testes e o histórico de vitórias/derrotas. Os dados são lidos diretamente do `PlayerData`, que por sua vez foi carregado do save JSON.

---

## Teclas de Debug (ExplorationState)

| Tecla | Função |
|---|---|
| `F2` | Mostra/esconde retângulos de colisão (player, edifícios, tráfego) |
| `F3` | Mostra/esconde zonas de trigger |

---

## Notas Técnicas

- O mundo não usa tiles — todos os elementos são sprites posicionados com coordenadas absolutas
- Colisões são todas AABB (Rectangle.Intersects)
- A câmara usa `Matrix.CreateTranslation` passada ao `SpriteBatch.Begin(transformMatrix:)`
- O save usa escrita atómica com ficheiro temporário e backup
- O `GameStateManager` usa o padrão Singleton com `Instance` estático
- Cada estado implementa a interface `IGameState` com `Initialize`, `LoadContent`, `Update`, `Draw` e `UnloadContent`
- O dinheiro **nunca é removido** por perder uma corrida — apenas se ganha menos
