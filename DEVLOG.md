# DEVLOG — Projeto "Recomeço"

Este documento registra o progresso do desenvolvimento do jogo e o **estado atual** para TCC / entrega.

| | |
|---|---|
| **Engine** | Unity 6 (URP) |
| **Plataforma** | PC |
| **Gênero** | Simulação / economia / sobrevivência leve |
| **Tema** | Recomeço após precariedade (moradia, renda informal, necessidades básicas) |
| **Referência de loop** | Schedule I (economia informal e progressão) |

Documentação complementar: [README.md](README.md) (como abrir o projeto e cenas principais).

---

# DIA 1 — Estrutura inicial do projeto 

Configuração do projeto na Unity.

Principais ações:

* criação do projeto
* criação da cena principal
* importação de assets básicos
* configuração do player na cena

Resultado:

O jogo já possui um personagem no mapa.

---

# DIA 2 — Sistema de movimentação do player 

Implementação do controle básico do personagem.

Funcionalidades:

* movimentação WASD
* controle de câmera com mouse
* sistema básico de locomoção em terceira pessoa

Scripts principais:

PlayerController.cs

Resultado:

O jogador consegue andar pelo mapa livremente.

---

# DIA 3 — Sistema de interação

Criação do sistema de interação com objetos.

Mecânica:

Player se aproxima de um objeto
↓
Mensagem aparece na tela
↓
Player pressiona tecla E
↓
A ação é executada

Componentes:

* InteractionText na UI
* Trigger nos objetos interativos

Resultado:

O jogador consegue interagir com objetos do mundo.

---

# DIA 4 — Sistema de itens

Criação do sistema de itens coletáveis.

Itens iniciais:

Latinha reciclável.

Scripts criados:

ItemData.cs
ItemPickup.cs

Funcionalidades:

* itens possuem dados próprios
* itens podem ser coletados
* itens desaparecem após coleta

Resultado:

O jogador consegue pegar itens no mapa.

---

# DIA 5 — Sistema de inventário

Implementação do inventário do jogador.

Características:

* inventário baseado em slots
* 12 slots disponíveis
* itens aparecem com ícone
* suporte a stack de itens iguais

Scripts principais:

Inventory.cs
InventorySlot.cs
SlotUI.cs
InventoryUI.cs

Resultado:

Itens coletados aparecem no inventário.

---

# DIA 6 — Sistema de stack de itens

Melhoria no inventário.

Funcionalidade:

Itens iguais se acumulam no mesmo slot.

Exemplo:

Latinha x5

Limite atual:

Stack máximo de 20 itens.

Resultado:

Inventário mais organizado e funcional.

---

# DIA 7 — Correções e estabilidade

Correções realizadas:

* erros de NullReferenceException
* problemas de UI não atualizando
* correções de scripts duplicados
* ajustes no SlotUI

Resultado:

Inventário funcionando corretamente.

---

# DIA 8 — Movimentação completa e sistema de dinheiro

**Movimentação (Fase 1 concluída):**

* andar (WASD), correr (Shift), pular (Space), agachar (Ctrl)
* uso de CharacterController com gravidade
* script: PlayerMovement.cs

**Sistema de dinheiro (Fase 5):**

* MoneyManager.cs — singleton com AddMoney(), RemoveMoney(), GetMoney()
* MoneyUI.cs — componente opcional para exibir dinheiro
* HUD atualizado para mostrar dinheiro a partir do MoneyManager (dinheiro inicial configurável, ex.: R$ 420)

Resultado:

Personagem com movimento completo; dinheiro exibido no HUD e centralizado em um único sistema.

---

# DIA 9 — Sistema de venda e integração com inventário

**Venda (Fase 6):**

* SellItems.cs — zona de venda (ferro velho) usa o inventário do jogador e o MoneyManager
* venda por nome de item (ex.: "Latinha") e preço por unidade configurável
* Inventory: GetItemCount() e RemoveItem() para contar e remover itens por nome

**Unificação:**

* HUD de latinhas passa a ler do inventário (GetItemCount("Latinha")), não mais do GameManager
* SlotUI ajustado para aceitar slot vazio (SetItem null)
* Coleta atualiza apenas o inventário; venda remove do inventário e adiciona dinheiro via MoneyManager

Resultado:

Loop completo: explorar → coletar → inventário → vender no ferro velho → ganhar dinheiro.

---

# DIA 10 — Mensagens de interação e conclusão do MVP

**Mensagens ao se aproximar/afastar:**

* ItemPickup: "Aperte E para coletar" ao entrar no trigger; mensagem some ao sair
* SellItems: "Aperte E para vender" (ou texto configurável) ao entrar na área de venda; mensagem some ao afastar
* Uso do InteractionUI existente (ShowText / HideText)

Resultado:

Feedback claro para o jogador em coleta e venda; MVP do loop principal concluído.

---

# FASE 11 — Cidade, lojas, NPCs e revenda

* Cena **Cidade** (low-poly urbano) e **Ferro Velho** (junkyard) com transição por portal / táxi.
* **Lojinha**, **lanchonete (FOOD4U)**, barraca de comida de rua, armazenamento.
* **Minigame de venda** a pedestres (`SellMinigameUI`) — faixa verde, reputação, dicas ao errar.
* **Revenda** de itens comprados na lojinha (meta em dinheiro para avançar missões).
* **SpawnManager** — latinhas na Cidade; evita spawn sobre água.

---

# FASE 12 — Necessidades, hospital e reputação

* **PlayerNeeds** — fome, vida, reputação, **proteção**, **doença** (exposição).
* Fome e corrida drenam necessidades; fome zero reduz vida; desmaio → **Hospital** (cena/spawn, conta, recuperação parcial).
* Reputação afeta confiança na venda na calçada.
* HUD de necessidades (`PlayerNeedsHud`, relógio, ciclo dia/noite).

Configuração central: `Assets/Resources/RecomecoGameplaySettings.asset`.

---

# FASE 13 — Moradia precária vs recomeço (casa)

| Moradia precária (barraca / lugar abandonado) | Casa (após compra) |
|-----------------------------------------------|---------------------|
| Dormir **à noite** (`PrecariousSleepInteract`) | **Cama** (`BedSaveInteract`) |
| Recuperação **parcial**; perde proteção; ganha doença | Descanso **seguro**; melhora proteção; reduz doença |
| Vídeo/fade opcional na barraca | **Salvar** de dia; **dormir + salvar** de noite |
| Sem “lar definitivo” na narrativa de missões | Missão **RestInSafeBed** |

* **GameplayDayNightCycle** — noite 20h–6h (configurável), `sleptThisNight`, transições ao dormir.
* **HouseDoorInteract** / compra de casa ligada à cadeia de missões.

---

# FASE 14 — Missões guiadas e save

* **MissionProgress** — arco completo até `AllComplete` (versão de save `MissionSchemaVersion = 2`).
* Início **Ferro Velho**: latinhas → ferro velho → ir à cidade.
* Início **Cidade**: conhecer moradia inicial → lojinha → revenda → lanchonete → comer → barraca → funding casa → comprar casa → cama.
* **SaveGameManager** — dinheiro, inventário, missão, necessidades, dia/noite, flags de sono.
* **MissionPanelUI**, localizador de objetivos, textos em linguagem simples (“lanchonete”, não siglas internas).

Scripts principais: `MissionProgress.cs`, `MissionTracker.cs`, `SaveGameData.cs`.

---

# FASE 15 — Menu, intro e bootstrap de cenas

* **MenuInicial** — JOGAR, escolha Ferro Velho ou Cidade, intro em vídeo.
* **MainMenuController**, spawn correto (`FerroVelhoInitialSpawnBootstrap`, `MoradiaInitialSpawnBootstrap`).
* **GameplaySceneRuntimeSetup** — player, HUD, missões, hospital ao acordar.
* Créditos de assets (`RecomecoCredits`).

---

# FASE 16 — Cidade viva (ambiente)

* **Tráfego** — rotas (`TrafficRoute`), spawn em `ActiveCityTraffic` (`CityTrafficManager`, `TrafficRouteFollower`).
* Carros **estacionados** na pasta `Vehicles` (decoração + colisão); tráfego **em movimento** só no pool ativo.
* **Luzes de poste** (`CityStreetLight`, `CityStreetLightManager`) — ativação por proximidade ao jogador.
* **Água** — shader Houidi (canal), barreiras invisíveis (`Recomeco_WaterWalkBlockers`); lago opcional com `LakeWaterZone` (efeito subaquático visual).
* Áudio ambiente e passos por superfície (`FootstepSurfaceResolver`, `AmbientAudioController`).

Menus Editor (versão enxuta): ver tabela em [README.md](README.md#ferramentas-de-setup-editor). Removidos: correções URP one-shot, terreno natureza automático, Mixamo, gerador de ícones FOOD4U, duplicatas de rotas e entradas legadas de menu/ferro velho.

---

# PILARES DE DESIGN (referência TCC)

1. **Renda informal** — coleta, ferro velho, revenda, venda a NPC; dinheiro sempre escasso no início.
2. **Corpo exposto** — fome, doença e proteção; dormir mal na rua não “resolve” o jogo.
3. **Moradia como progressão** — barraca = sobreviver; casa = recomeço simbólico e mecânico (save confortável, descanso).
4. **Cidade como sistema** — tempo (noite), tráfego e ambiente reforçam escala urbana, sem simular vida completa.

---

# MATRIZ MECÂNICA ↔ PRECARIEDADE (anexo sugerido)

| Mecânica | O que comunica |
|----------|----------------|
| Latinhas + ferro velho | Renda imediata, trabalho de baixo retorno |
| Revenda / meta R$ 4,00 | Capital mínimo para subsistir na cidade |
| Lanchonete + fome | Custo fixo de viver; pressão constante |
| Reputação na venda | Confiança social necessária para renda na rua |
| Proteção / doença | Exposição ao clima e à falta de abrigo |
| Dormir na barraca | Descanso precário, risco à saúde |
| Comprar casa + cama | Estabilidade habitacional como objetivo jogável |
| Hospital ao desmaiar | Consequência de negligenciar necessidades |
| Missões em linguagem clara | Guia quem não conhece o gênero / o tema |

---

# ESTADO ATUAL DO PROJETO

## Sistemas implementados e jogáveis

* Locomoção terceira pessoa (`CharacterMover`, câmera, snap ao chão).
* Interação (E), inventário, itens, dinheiro (`MoneyManager`).
* Ferro velho, lojinha, lanchonete, comida, armário, portas de casa.
* NPC venda calçada, reputação, dicas de gameplay.
* Necessidades completas + desmaio/hospital.
* Ciclo dia/noite + dormir barraca vs cama + save.
* Cadeia de missões até conclusão + persistência.
* Menu, múltiplas cenas, settings centralizados.
* Tráfego, luzes urbanas, água estilizada, barreiras no canal.

## Loop principal (pós-MVP)

**Ferro Velho:** explorar → coletar latinhas → vender → ir à cidade.

**Cidade:** moradia precária → trabalhar economia informal (loja, revenda, rua, lanchonete) → juntar para casa → **recomeço** na cama segura.

## O que fica em sandbox após `AllComplete`

Exploração livre na Cidade com sistemas ativos (fome, tráfego, etc.), sem novo arco de missões — **limitação assumida** para o TCC.

---

# CENAS PRINCIPAIS

| Cena | Papel |
|------|--------|
| `MenuInicial` | Menu e escolha de início |
| `FerroVelho` | Tutorial econômico inicial (latinhas) |
| `Cidade` | Mundo principal urbano |
| `Interior Casa elegante (player)` | Interior da casa do jogador |
| `Gameplay_City`, `Gameplay_Test`, etc. | Cenas auxiliares / testes |

IDs de spawn e nomes constantes: `RecomecoSceneNames.cs`.

---

# PRÓXIMOS PASSOS (entrega TCC — não features obrigatórias)

1. **Playtest estruturado** (5–10 pessoas, roteiro, questionário curto).
2. **Build PC** de demonstração + teste de save/load fora do Editor.
3. **Monografia** — problema, referencial, método, matriz acima, limitações, avaliação.
4. **Atualizar este DEVLOG** após cada marco de playtest ou correção crítica.
5. Opcional: tela ou texto curto de **epílogo** ao completar missões (reforço narrativo).

## Trabalhos futuros (fora do escopo atual)

* Natação, barco, emprego formal, mapa maior, IA social avançada.
* Pós-jogo profundo além do sandbox.

---

# OBSERVAÇÕES

O projeto **superou o MVP de latinha + ferro velho**: é um **vertical slice** jogável alinhado ao tema de precariedade e recomeço habitacional.

O histórico **DIA 1–10** (início deste arquivo) documenta a fundação técnica; as **FASES 11–16** descrevem a evolução até o estado atual.

Para abrir ajustes de balanceamento: menu **Recomeco → Abrir Gameplay Settings**.
