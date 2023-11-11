using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private Transform respawnPoint;

    private Dictionary<Character, int> stocks;
    
    private void Start()
    {
        stocks = new Dictionary<Character, int>();
        
        Character.OnCreated += AddCharacterToDictionary;
        Character.OnDeath += RespawnCharacter;
    }
    
    private void AddCharacterToDictionary(Character character)
    {
        stocks.Add(character, 3);
    }

    private void RespawnCharacter(Character character)
    {
        character.transform.position = respawnPoint.position;
        character.Respawn();
        
        if(!stocks.ContainsKey(character)) AddCharacterToDictionary(character);
        stocks[character]--;
        Debug.Log("Stocks left: " + stocks[character]);
    }
}
