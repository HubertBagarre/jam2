using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    public static event Action OnFirstUltiProc;
    [Serializable]
    public class PlayerOptions
    {
        [field: SerializeField] public string Name { get; private set; }
        [field: SerializeField] public CombatModel NormalModel{ get; private set; }
        [field: SerializeField] public CombatModel TransformedModel{ get; private set; }
    }
    
    [Header("Game Settings")]
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private Transform[] spawnPoints;
    private List<Transform> availableSpawnPoints = new ();
    [SerializeField] private PlayerOptions[] playerOptions;
    [SerializeField] private int stocksPerCharacter = 3;
    [SerializeField] private float respawnDuration = 3;
    [SerializeField] private int minPlayers = 2;

    [Header("Components")]
    [SerializeField] private Transform playerSelectionLayout;
    [SerializeField] private UIPlayerSelection playerSelectionPrefab;
    [SerializeField] private Transform playerPercentLayout;
    [SerializeField] private UIPlayerPercent playerPercentPrefab;
    [SerializeField] private GameObject endGamePanel;
    [SerializeField] private TextMeshProUGUI endGameText;
    [SerializeField] private Button endGameButton;
    [Space]
    [SerializeField] private GameObject knockFXPrefab;
    
    
    private List<MagicalGirlController> controllers = new ();
    private Dictionary<MagicalGirlController, (Action unbindAction,bool isReady,int playerOptionIndex)> controllerDict = new ();
    
    private static Dictionary<Character, int> stocks;
    
    private List<Character> characters = new ();
    
    private void Start()
    {
        Application.targetFrameRate = 60;
        
        endGamePanel.SetActive(false);
        
        availableSpawnPoints.AddRange(spawnPoints);
        
        stocks = new Dictionary<Character, int>();
        
        Character.OnCreated += SpawnCharacter;
        Character.OnDeath += RespawnCharacter;
        
        MagicalGirlController.OnJoinedGame += AddController;
        
        endGameButton.onClick.AddListener(ReturnToMenu);

        return;
        
        void ReturnToMenu()
        {
            UnbindAll();
            
            Character.OnCreated -= SpawnCharacter;
            Character.OnDeath -= RespawnCharacter;
            SceneManager.LoadScene(0);
        }
    }
    
    private void AddController(MagicalGirlController controller)
    {
        if(controllers.Contains(controller)) return;
        controllers.Add(controller);
        BindControlForMenu(controller);
    }

    private void UnbindAll()
    {
        foreach (var v in controllerDict.Values)
        {
            v.unbindAction?.Invoke();
        }
    }

    private void BindControlForMenu(MagicalGirlController controller)
    {
        var playerSelection = Instantiate(playerSelectionPrefab,playerSelectionLayout);
        var currentDir = 0;
        
        controller.Input.actions["Move"].started += ChangeCharacter;
        controller.Input.actions["Move"].performed += ChangeCharacter;
        controller.Input.actions["Move"].canceled += ChangeCharacter;
        
        controller.Input.actions["Jump"].started += ToggleReady;
        
        controllerDict.Add(controller,(UnbindAction,false,0));
        playerSelection.Text.text = controllerDict[controller].isReady ? "Ready" : "Not Ready";
        
        return;
        
        void UnbindAction()
        {
            controller.Input.actions["Move"].started -= ChangeCharacter;
            controller.Input.actions["Move"].performed -= ChangeCharacter;
            controller.Input.actions["Move"].canceled -= ChangeCharacter;

            controller.Input.actions["Jump"].started -= ToggleReady;
        }
        
        void ChangeCharacter(InputAction.CallbackContext context)
        {
            var value = context.ReadValue<Vector2>();
            
            var ready = controllerDict[controller].isReady;
            var playerOptionIndex = controllerDict[controller].playerOptionIndex;

            switch (value.x)
            {
                case > 0:
                    if(currentDir > 0.5f) return;
                    playerOptionIndex++;
                    currentDir = 1;
                    break;
                case < 0:
                    if(currentDir < -0.5f) return;
                    playerOptionIndex--;
                    currentDir = -1;
                    break;
                default:
                    currentDir = 0;
                    return;
            }

            if(playerOptionIndex < 0) playerOptionIndex = playerOptions.Length - 1;
            if(playerOptionIndex >= playerOptions.Length) playerOptionIndex = 0;
            
            controllerDict[controller] = (UnbindAction,ready,playerOptionIndex);
            
            playerSelection.NameText.text = playerOptions[playerOptionIndex].Name;
        }

        
        void ToggleReady(InputAction.CallbackContext context)
        {
            var ready = controllerDict[controller].isReady;
            var playerOptionIndex = controllerDict[controller].playerOptionIndex;
            controllerDict[controller] = (UnbindAction,!ready,playerOptionIndex);
            
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
        
        controllerDict[controller] = (UnbindAction,controllerDict[controller].isReady,controllerDict[controller].playerOptionIndex);
        
        return;
        void UnbindAction()
        {
            controller.Input.actions["Move"].started -= controller.Move;
            controller.Input.actions["Move"].performed -= controller.Move;
            controller.Input.actions["Move"].canceled -= controller.Move;
        
            controller.Input.actions["Jump"].started -= controller.Jump;
            controller.Input.actions["Jump"].canceled -= controller.Jump;
        
            controller.Input.actions["LightAttack"].started -= controller.LightAttack;
            controller.Input.actions["LightAttack"].canceled -= controller.LightAttack;
        
            controller.Input.actions["HeavyAttack"].started -= controller.HeavyAttack;
            controller.Input.actions["HeavyAttack"].canceled -= controller.HeavyAttack;
        
            controller.Input.actions["Dodge"].started -= controller.Dodge;
            controller.Input.actions["Dodge"].canceled -= controller.Dodge;
        
            controller.Input.actions["Shield"].started -= controller.ShieldOrDash;
            controller.Input.actions["Shield"].canceled -= controller.ShieldOrDash;
        }
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

    private void DistribUltimate(Character character,float percent)
    {
        List<Character> otherCharacters = stocks.Keys.Where(c => c != character).ToList();
        if(otherCharacters.Count == 0) return;
        float curentTotalPercent = 0;

        foreach (var ch in otherCharacters)
        {
            curentTotalPercent = ch.GainUltimate(percent);
        }
        
        if(curentTotalPercent >= 1)
        {
            OnFirstUltiProc?.Invoke();
            foreach (var ch in otherCharacters)
            {
                ch.OnGainUltimate -= DistribUltimate;
            }
            character.OnGainUltimate -= DistribUltimate;
        }
    }
    
    private void SetupCharacter(MagicalGirlController controller)
    {
        var character = controller.SpawnCharacter();

        character.ApplyPlayerOptions(playerOptions[controllerDict[controller].playerOptionIndex]);
        character.OnGainUltimate += DistribUltimate;
        
        var playerPercent = Instantiate(playerPercentPrefab,playerPercentLayout);

        playerPercent.SetupStocks(stocksPerCharacter);
        
        character.OnPercentChanged += playerPercent.UpdatePercent;
        character.OnTransformationChargeUpdated += playerPercent.UpdateTransformationCharge;
        Character.OnDeath += DecreaseStocks;
        
        return;
        
        void DecreaseStocks(Character c)
        {
            if(character != c ) return;
            
            playerPercent.LoseStock();
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
        character.ShouldFollow = true;
    }

    private void RespawnCharacter(Character character)
    {
        var characterPos = character.transform.position;
        var knockFX = Instantiate(knockFXPrefab,characterPos,knockFXPrefab.transform.rotation);
        knockFX.transform.forward = (Vector3.zero - characterPos).normalized;
        
        if(!stocks.ContainsKey(character)) SpawnCharacter(character);
        stocks[character]--;
        
        character.ShouldFollow = false;
        if (stocks[character] > 0 || stocks.Count <= 1)
        {
            character.Respawn(respawnDuration,respawnPoint.position);
        }
        
        if(stocks.Count(stock => stock.Value > 0) > 1 || stocks.Count <= 1) return;
        
        var winner = stocks.FirstOrDefault(stock => stock.Value > 0).Key;

        endGameText.text = $"Winner : {winner}";
        
        endGamePanel.SetActive(true);
    }
}
