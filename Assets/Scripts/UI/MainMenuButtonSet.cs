using System;
using UnityEngine;

[CreateAssetMenu(fileName = "MainMenuButtonSet", menuName = "Recomeco/Main Menu Button Set")]
public class MainMenuButtonSet : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public Sprite normal;
        public Sprite hover;
        [Tooltip("Opcional — ex.: JOGAR amarelo quando selecionado.")]
        public Sprite selected;
    }

    public Entry jogar;
    public Entry opcoes;
    public Entry creditos;
    public Entry sair;

    public Entry Get(int index)
    {
        return index switch
        {
            0 => jogar,
            1 => opcoes,
            2 => creditos,
            3 => sair,
            _ => default,
        };
    }
}
