GAME DESIGN DOCUMENT (GDD)
Informações Gerais
Título provisório: Recomeço
 Plataforma: PC
 Engine: Unity
 Gênero: Simulação / Economia / Sandbox
 Perspectiva: Terceira pessoa (3D)
Referência principal de gameplay:
 Schedule I (sistema de progressão e venda de itens)

1. Visão Geral do Jogo
O jogador começa como um morador de rua que recebe uma pequena quantia de dinheiro de um desconhecido. Esse dinheiro representa a única oportunidade de mudar de vida.
O objetivo do jogador é multiplicar esse dinheiro através de comércio informal, comprando, coletando ou produzindo itens e vendendo para NPCs pela cidade.
Ao longo do jogo, o jogador pode evoluir de:
morador de rua


vendedor ambulante


dono de barraca


pequeno comerciante (dono de um bar)


O jogo simula economia de rua, escolhas financeiras e progressão social.

2. Objetivo do Jogo
Objetivo principal:
Sair da situação de pobreza e alcançar estabilidade financeira.
Metas possíveis:
conseguir uma moradia


comprar veículos (Skate, bicicleta, carro, caminhonete)


abrir um pequeno negócio (Bar)


acumular riqueza



3. Loop Principal de Gameplay
O loop principal define a experiência do jogador.
Ciclo de gameplay:
Explorar a cidade


Encontrar itens ou comprar produtos


Armazenar no inventário


Encontrar clientes


Vender itens


Obter lucro


Investir em melhorias


Representação do loop:
Explorar → Comprar/Coletar → Vender → Lucrar → Expandir

4. História Inicial
Cena de abertura:
O personagem acorda em um banco de praça após dormir na rua.
Um NPC desconhecido passa e deixa uma pequena quantia de dinheiro.
Mensagem:
“Use isso com sabedoria. Pode ser sua única chance.”
Objetivo inicial:
Transformar R$420 em R$1000
Táticas de venda: Se vestir de maneira adequada para conquistar mais clientes.
(A chance de recusarem a compra vai ser maior se estiver vestido de maneira mais desleixada)

5. Mecânicas Principais
Movimento
O jogador pode:
andar


correr


pular
Agachar


interagir com objetos


Controle padrão:
WASD – movimentação
 Mouse – câmera
 E – interagir
shift - correr
ctrl agachar

Sistema de Inventário
O personagem possui um inventário limitado.
Características:
número máximo de itens (existem 3 niveis de mochila, o inventário aumenta dependendo da mochila equipada)


organização por slots (itens repetidos tem sistema de stack limitado a 20 itens)


Itens ocupam espaço na mochila.

Sistema de Dinheiro
O dinheiro é utilizado para:
comprar produtos e roupas


pagar comida


comprar equipamentos


investir em melhorias



Sistema de Compra e Venda
Itens possuem:
preço de compra


preço médio de venda


variação de demanda








Exemplo:
Item
Compra
Venda
     Água
1
3
     Bala
0.5
2
Camiseta usada
10
25


6. Tipos de Produtos
Nível inicial (oferecer para as pessoas na rua)
latinhas recicláveis


garrafas


água


bala


Nível intermediário (barraca própria)
comida de rua


roupas usadas


eletrônicos usados


Nível avançado (estabelecimento - bar)
itens em atacado


produtos importados


bebidas



7. Sistema de NPCs
NPCs circulam pela cidade.
Eles possuem comportamentos diferentes:
comprador fácil


negociador


cliente exigente


Interações possíveis:
oferecer produto


negociar preço


recusar venda



8. Sistema de Reputação (barra de reputação na HUD)
A reputação do jogador influencia nas vendas.
Boa reputação:
clientes pagam mais


maior confiança


Má reputação:
clientes recusam compra


preços menores



9. Sistema de Necessidades
O personagem possui necessidades básicas.
Barras principais:
fome


energia


saúde


estresse


Se ignoradas:
perda de Saúde



10. Sistema de Alcoolismo
O jogador pode consumir álcool.
Efeitos:
Curto prazo:
reduz estresse


Longo prazo:
diminui energia


prejudica decisões



11. Progressão do Personagem
O jogador evolui gradualmente.
Início
sem moradia


mochila pequena


poucos itens
vende apenas oferecendo aos clientes


Meio do jogo
bicicleta / skate


barraca


Final
loja própria (Bar)
funcionários NPC
comprar mais estabelecimentos para administrar.



12. Mapa do Jogo
Cidade pequena com áreas distintas.
Locais principais:
praça


praia


mercado


ferro velho


bairro residencial


terminal de ônibus


Cada área possui diferentes tipos de clientes e demanda.

13. Estilo Visual
Estilo gráfico:
Low poly
Motivos:
performance leve


produção rápida


estilo visual consistente


Características:
modelos simples


texturas básicas


iluminação simples



14. Áudio
Tipos de áudio:
Ambiente:
carros


pessoas conversando


vento


cidade


Efeitos:
passos


moedas


porta


interação com NPC



15. Interface (UI)
Elementos principais:
dinheiro atual


inventário


barras de necessidade


mini mapa


interação com NPC



16. MVP (Primeira Versão Jogável)
Primeira versão do jogo deve conter apenas:
personagem andando


coleta de latinhas


venda no ferro velho


sistema de dinheiro


Sem:
veículos


reputação


necessidades


alcoolismo


Esses sistemas podem ser adicionados posteriormente.

17. Estrutura Técnica (Unity)
Principais scripts:
Arquitetura de Scripts

Inventory System
- Inventory.cs
- InventorySlot.cs
- SlotUI.cs
- InventoryUI.cs

Item System
- ItemData.cs
- ItemPickup.cs

UI System
- InteractionUI.cs
- HUDController.cs

Economy System 
- MoneySystem.cs
- SellZone.cs




18. Riscos do Projeto
Principais desafios:
sistema de economia


interação com NPCs


balanceamento de preços


escopo grande para um desenvolvedor iniciante


Mitigação:
começar com protótipos simples


adicionar sistemas gradualmente



19. Escopo Recomendado
Para um desenvolvedor iniciante:
mapa pequeno


10 tipos de itens


5 NPCs


1 sistema de venda
