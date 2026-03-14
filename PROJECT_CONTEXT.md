# Contexto do Projeto

Este é um projeto de jogo feito na Unity usando C#.

O desenvolvedor é iniciante e não tem muita experiência com Unity, então todas as explicações devem ser claras, simples e passo a passo.

## Objetivo do jogo

O jogador explora uma cidade coletando lixo reciclável (como latinhas) e vendendo esses itens para ganhar dinheiro.

O jogo é inspirado em sistemas de progressão e coleta de itens.

## Sistemas já implementados

* Movimentação do jogador
* Sistema de interação com tecla E
* Sistema de coleta de itens
* Inventário visual com 12 slots
* Stack de itens iguais
* Sistema de UI com Canvas

Estrutura atual da UI:

Canvas

* HUD
* interactionText
* inventoryUI

  * slots
  * icons

## Estrutura de scripts

Assets/Scripts/

Inventory

* Inventory.cs
* InventorySlot.cs
* SlotUI.cs

Items

* ItemPickup.cs

## Regras para modificar o projeto

Quando gerar ou modificar código:

1. Sempre gerar scripts completos
2. Nunca modificar arquivos .meta
3. Nunca modificar arquivos dentro da pasta Library
4. Nunca modificar cenas .unity
5. Sempre explicar onde colocar cada script
6. Sempre explicar o que o código faz

## Próximos sistemas planejados

* Sistema de dinheiro no HUD
* Sistema de venda de itens
* NPC reciclador
* Sistema de spawn de lixo
* Sistema de melhorias para o jogador

Sempre sugerir implementações simples e seguras.
