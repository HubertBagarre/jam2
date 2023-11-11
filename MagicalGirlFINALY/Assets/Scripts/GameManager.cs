using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private Transform[] spawnPoints;
    private List<Transform> availableSpawnPoints = new ();
    [SerializeField] private int stocksPerCharacter = 3;
    [SerializeField] private int minPlayers = 2;

    [Header("Components")]
    [SerializeField] private Transform playerSelectionLayout;
    [SerializeField] private UIPlayerSelection playerSelectionPrefab;
    [SerializeField] private Transform playerPercentLayout;
    [SerializeField] private UIPlayerPercent playerPercentPrefab;
    
    
    private List<MagicalGirlController> controllers = new ();
    private Dictionary<MagicalGirlController, (Action unbindAction,bool isReady)> controllerDict = new ();
    
    
    
    private Dictionary<Character, int> stocks;
    
    private void Start()
    {
        Application.targetFrameRate = 60;
        
        availableSpawnPoints.AddRange(spawnPoints);
        
        stocks = new Dictionary<Character, int>();
        
        Character.OnCreated += SpawnCharacter;
        Character.OnDeath += RespawnCharacter;
        
        MagicalGirlController.OnJoinedGame += AddController;
    }
    
    private void AddController(MagicalGirlController controller)
    {
        if(controllers.Contains(controller)) return;
        controllers.Add(controller);
        BindControlForMenu(controller);
    }

    private void BindControlForMenu(MagicalGirlController controller)
    {
        var playerSelection = Instantiate(playerSelectionPrefab,playerSelectionLayout);
        
        controller.Input.actions["Move"].started += ChangeCharacter;
        controller.Input.actions["Move"].performed += ChangeCharacter;
        controller.Input.actions["Move"].canceled += ChangeCharacter;
        
        controller.Input.actions["Jump"].started += ToggleReady;
        
        controllerDict.Add(controller,(UnbindAction,false));
        playerSelection.Text.text = controllerDict[controller].isReady ? "Ready" : "Not Ready";
        
        return;
        
        void UnbindAction()
        {
            Debug.Log("Unbinding");
            
            controller.Input.actions["Move"].started -= ChangeCharacter;
            controller.Input.actions["Move"].performed -= ChangeCharacter;
            controller.Input.actions["Move"].canceled -= ChangeCharacter;

            controller.Input.actions["Jump"].started -= ToggleReady;
        }
        
        void ChangeCharacter(InputAction.CallbackContext context)
        {
            
        }

        
        void ToggleReady(InputAction.CallbackContext context)
        {
            var ready = controllerDict[controller].isReady;
            controllerDict[controller] = (UnbindAction,!ready);
            
            playerSelection.Text.text = controllerDict[controller].isReady ? "Ready" : "Not Ready";

            StartGame();
        }
    }

    private void BindControlsForGame(MagicalGirlController controller)
    {
        controller.Input.actions["Move"].started += controller.Move;
        controller.Input.actions["Move"].performed += controller.Move;
        controller.Input.actions["Move"].canceled += controller.Move;
        
        controller.Input.actions["Jump"].started += controller.Jump;
        controller.Input.actions["Jump"].canceled += controller.Jump;
        
        controller.Input.actions["LightAttack"].started += controller.LightAttack;
        controller.Input.actions["LightAttack"].canceled += controller.LightAttack;
        
        controller.Input.actions["HeavyAttack"].started += controller.HeavyAttack;
        controller.Input.actions["HeavyAttack"].canceled += controller.HeavyAttack;
        
        controller.Input.actions["Dodge"].started += controller.Dodge;
        controller.Input.actions["Dodge"].canceled += controller.Dodge;
        
        controller.Input.actions["Shield"].started += controller.ShieldOrDash;
        controller.Input.actions["Shield"].canceled += controller.ShieldOrDash;
    }

    private void StartGame()
    {
        if(controllerDict.Values.Any(tuple => tuple.isReady == false) || controllerDict.Count < minPlayers) return;
        
        MagicalGirlController.OnJoinedGame -= AddController;
        
        playerSelectionLayout.gameObject.SetActive(false);
        
        foreach (var action in controllerDict.Values.Select(tuple => tuple.unbindAction))
        {
            action?.Invoke();
        }
        
        foreach (var controller in controllers)
        {
            BindControlsForGame(controller);
            
            SetupCharacter(controller);
        }
    }

    private void SetupCharacter(MagicalGirlController controller)
    {
        var character = controller.SpawnCharacter();
        
        var playerPercent = Instantiate(playerPercentPrefab,playerPercentLayout);

        character.OnPercentChanged += UpdatePercent;
        
        return;
        
        void UpdatePercent(int previous,int percent)
        {
            playerPercent.PercentText.text = $"{percent}%"; //TODO anim stylé
        }
    }
    
    private void SpawnCharacter(Character character)
    {
        if(stocks.ContainsKey(character)) return;
        stocks.Add(character, stocksPerCharacter);
        
        if(availableSpawnPoints.Count == 0) availableSpawnPoints.AddRange(spawnPoints);
        var pos = availableSpawnPoints[Random.Range(0, availableSpawnPoints.Count)];
        availableSpawnPoints.Remove(pos);
        
        character.transform.position = pos.position;
    }

    private void RespawnCharacter(Character character)
    {
        character.transform.position = respawnPoint.position;
        character.Respawn();
        
        if(!stocks.ContainsKey(character)) SpawnCharacter(character);
        stocks[character]--;
        Debug.Log("Stocks left: " + stocks[character]);
    }
}
