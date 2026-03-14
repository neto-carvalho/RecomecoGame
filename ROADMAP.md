# ROADMAP DE DESENVOLVIMENTO — JOGO "RECOMEÇO"

Este roadmap define a ordem de desenvolvimento do jogo.

O projeto é feito na Unity usando C#.

O desenvolvedor é iniciante, então todas as implementações devem ser simples, claras e seguras.

O assistente de IA deve ajudar a implementar cada etapa sem quebrar sistemas já existentes.

Sempre gerar scripts completos e explicar onde colocá-los no projeto.

---

# LOOP PRINCIPAL DO JOGO

Explorar cidade
↓
Coletar itens
↓
Armazenar no inventário
↓
Encontrar local de venda
↓
Vender itens
↓
Ganhar dinheiro
↓
Comprar melhorias

---

# MVP — PRIMEIRA VERSÃO JOGÁVEL

O MVP deve conter apenas os sistemas abaixo:

* movimentação do jogador
* coleta de latinhas
* inventário com slots
* venda no ferro velho
* sistema de dinheiro

Sistemas mais avançados serão adicionados depois.

---

# FASE 1 — SISTEMA DO PLAYER

Objetivo: criar um personagem jogável.

Funcionalidades:

* andar (WASD)
* câmera com mouse
* correr (Shift)
* pular (Space)
* agachar (Ctrl)

Scripts principais:

PlayerController.cs
PlayerCamera.cs

Requisitos:

* usar CharacterController ou Rigidbody
* código simples e bem comentado

---

# FASE 2 — SISTEMA DE INTERAÇÃO

Objetivo: permitir interação com objetos do mundo.

Funcionamento:

Player chega perto do objeto
↓
UI mostra "Pressione E"
↓
Player aperta E
↓
Interação acontece

Scripts:

InteractionSystem.cs
InteractionUI.cs

---

# FASE 3 — SISTEMA DE ITENS

Objetivo: criar itens coletáveis.

Itens iniciais:

* latinha
* garrafa
* água
* bala

Scripts:

ItemData.cs
ItemPickup.cs
ItemDatabase.cs

Requisitos:

ItemData deve usar ScriptableObject.

---

# FASE 4 — SISTEMA DE INVENTÁRIO

Objetivo: armazenar itens coletados.

Regras:

inventário baseado em slots
itens iguais fazem stack
stack máximo = 20

Scripts:

InventorySystem.cs
InventorySlot.cs
InventoryUI.cs
SlotUI.cs

Funções importantes:

AddItem()
RemoveItem()
UpdateUI()

---

# FASE 5 — SISTEMA DE DINHEIRO

Objetivo: controlar economia do jogador.

Scripts:

MoneyManager.cs
MoneyUI.cs

Funções:

AddMoney()
RemoveMoney()
GetMoney()

HUD deve mostrar dinheiro atual.

---

# FASE 6 — SISTEMA DE VENDA

Objetivo: permitir vender itens.

Local inicial:

ferro velho

Funcionamento:

player entra na área
↓
abre interface de venda
↓
itens são convertidos em dinheiro

Scripts:

SellSystem.cs
SellZone.cs

---

# FASE 7 — SPAWN DE ITENS NO MAPA

Objetivo: itens aparecerem pela cidade.

Scripts:

ItemSpawner.cs
SpawnManager.cs

Funcionalidade:

spawn aleatório
respawn após tempo

---

# FASE 8 — NPCS SIMPLES

Objetivo: criar NPCs que compram itens.

Comportamento inicial:

NPC parado
Player pode interagir
NPC compra itens

Scripts:

NPCController.cs
NPCInteraction.cs

---

# FASE 9 — MAPA DO MVP

Mapa inicial pequeno.

Áreas:

praça
ferro velho
mercado

Cada área pode ter tipos diferentes de itens.

---

# FASE 10 — LOOP COMPLETO FUNCIONANDO

Quando tudo estiver pronto:

Explorar mapa
↓
Coletar latinhas
↓
Inventário enche
↓
Ir ao ferro velho
↓
Vender
↓
Ganhar dinheiro

Se isso funcionar, o MVP está completo.

---

# FASE 11 — EXPANSÕES FUTURAS

Após MVP:

Sistema de reputação
Sistema de necessidades (fome, energia)
Sistema de veículos
Sistema de comércio avançado
Sistema de negócios (bar)

---

# REGRAS IMPORTANTES PARA O ASSISTENTE DE IA

Ao ajudar no desenvolvimento:

* sempre gerar scripts completos
* não modificar arquivos .meta
* não modificar arquivos dentro da pasta Library
* não modificar cenas .unity automaticamente
* explicar sempre onde colocar cada script
* implementar apenas uma fase por vez

---

# COMO O ASSISTENTE DEVE AJUDAR

Quando o usuário pedir ajuda:

1. analisar os scripts existentes
2. verificar qual fase do roadmap está sendo implementada
3. sugerir a próxima etapa segura
4. gerar código compatível com o projeto
