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

# ESTADO ATUAL DO PROJETO

Sistemas funcionando:

* movimentação completa do player (andar, correr, pular, agachar)
* sistema de interação com mensagens (coletar / vender)
* coleta de itens (Latinha)
* inventário com slots e stack
* sistema de dinheiro (MoneyManager, exibição no HUD)
* venda no ferro velho (itens do inventário → dinheiro)
* spawn de itens no mapa (SpawnManager)

Loop do MVP concluído:

Explorar → Coletar → Armazenar no inventário → Vender no ferro velho → Ganhar dinheiro

---

# PRÓXIMOS PASSOS

De acordo com o roadmap:

1. Fase 8 — NPCs simples (parados, jogador interage para vender)
2. Fase 9 — Mapa do MVP (praça, ferro velho, mercado com áreas distintas)
3. Fase 10 — Testes e polish do loop completo
4. Expansões futuras: reputação, necessidades, veículos, comércio avançado

---

# OBSERVAÇÕES

O projeto saiu da fase de protótipo: o loop principal do MVP está implementado e jogável.

Próximo foco: NPCs ou expansão do mapa, conforme prioridade do desenvolvimento.
