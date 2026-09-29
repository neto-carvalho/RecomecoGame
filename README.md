# Recomeço (RecomecoGame)

Jogo de simulação / economia informal para PC, desenvolvido em **Unity 6** com **URP**.  
Tema: **precariedade habitacional e recomeço** — da coleta no ferro velho à moradia precária na cidade e, por fim, um lugar seguro para dormir e salvar.

Documentação de desenvolvimento: **[DEVLOG.md](DEVLOG.md)** (estado atual, fases, matriz mecânica ↔ precariedade, roteiro TCC).

## Requisitos

* Unity **6000.0.x** (ver `ProjectSettings/ProjectVersion.txt`)
* Universal Render Pipeline (já configurado em `Assets/Settings/`)

## Como rodar

1. Abra a pasta do projeto no Unity Hub.
2. Cena de entrada: **`Assets/Scenes/MenuInicial.unity`**
3. Play — escolha **Ferro Velho** (arco desde latinhas) ou **Cidade** (arco desde moradia inicial).

## Cenas principais

| Arquivo | Uso |
|---------|-----|
| `MenuInicial.unity` | Menu |
| `FerroVelho.unity` | Início alternativo (reciclagem) |
| `Cidade.unity` | Mundo urbano principal |
| `Interior Casa elegante (player).unity` | Interior da casa |

## Configuração de gameplay

* **`Assets/Resources/RecomecoGameplaySettings.asset`** — fome, tráfego, dia/noite, moradia, debug.
* Menu do Editor: **Recomeco → Abrir Gameplay Settings**

## Ferramentas de setup (Editor)

Menu **`Recomeco/`** (itens one-shot de import URP / arte do menu foram removidos).

| Menu | Uso |
|------|-----|
| **Abrir Gameplay Settings** | Balanceamento global |
| **Cidade →** Configurar cidade viva; rotas de tráfego; hospital; postes **Pole**; vídeo/céu barraca |
| **Cidade → Tráfego** (submenus em Tráfego) | Gerar rede, suavizar rotas, limpar rotas geradas |
| **Água →** Aplicar Houidi; barreiras invisíveis |
| **Loja →** Lojinha, FOOD4U, latinhas |
| **Casas →** Casa elegante (porta, interior, cama) |
| **Veículos →** Colisão estacionados; marcar estacionado |
| **Cenas →** Menu, moradia/barraca, build settings, completar player |
| **Debug →** Dinheiro de teste (Editor) |

Detalhes: [DEVLOG.md](DEVLOG.md).

## Estrutura de código (resumo)

| Área | Pastas / scripts |
|------|------------------|
| Missões | `Assets/Scripts/UI/MissionProgress.cs` |
| Necessidades / save | `Assets/Scripts/Systems/PlayerNeeds.cs`, `SaveGameManager` |
| Dia/noite / sono | `GameplayDayNightCycle`, `PrecariousSleepInteract`, `BedSaveInteract` |
| Economia | `MoneyManager`, `ShopZone`, `SellMinigameUI`, ferro velho |
| Tráfego | `Assets/Scripts/Vehicles/` |
| Cenas | `RecomecoSceneNames.cs` |

## Build

Inclua cenas do menu e gameplay no **Build Settings**.  
Prefabs de tráfego em runtime: **`Assets/Resources/CityTraffic/`** (menu **Recomeco → Cidade → Copiar prefabs de tráfego para Resources**).

## Licença e créditos

Assets de terceiros (Kenney, Flat Style Vehicles, packs urbanos, etc.) — ver **`RecomecoCredits`** no jogo e pastas de assets no projeto.

## Repositório

Não versionar pastas geradas: `Library/`, `Temp/`, `Logs/`, `UserSettings/` (ver `.gitignore`).
