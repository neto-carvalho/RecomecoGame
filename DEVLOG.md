# DEVLOG — Projeto "Recomeço"

Este documento registra o progresso do desenvolvimento do jogo.

Engine: Unity
Plataforma: PC
Gênero: Simulação / Economia / Sandbox

Referência principal de gameplay:
Schedule I

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

# ESTADO ATUAL DO PROJETO

Sistemas funcionando:

* movimentação do player
* sistema de interação
* coleta de itens
* inventário com slots
* stack de itens

Sistema em desenvolvimento:

* sistema de dinheiro
* sistema de venda de itens

---

# PRÓXIMOS PASSOS

De acordo com o roadmap do projeto:

1. implementar sistema de dinheiro
2. implementar venda de itens
3. criar ferro velho para vender latinhas
4. criar spawn de itens no mapa

---

# OBSERVAÇÕES

O projeto ainda está na fase de protótipo.

O foco atual é construir o loop principal do jogo:

Explorar → Coletar → Vender → Lucrar
