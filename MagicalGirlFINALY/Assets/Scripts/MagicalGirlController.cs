using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class MagicalGirlController : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Character characterPrefab;

    [Header("Settings")]
    [SerializeField] private Quaternion spawnRotation;
    
    [field: Header("Debug")]
    [field:SerializeField] public Vector2 StickInput { get; private set; }
    
    private Character character;
    private bool hasCharacter;
    
    [field:SerializeField] public bool JumpPressed { get; private set; }
    [field:SerializeField] public bool LightAttackPressed { get; private set; }
    [field:SerializeField] public bool HeavyAttackPressed { get; private set; }
    [field:SerializeField] public bool DodgePressed { get; private set; }
    
    private void Start()
    {
        SpawnCharacter();
    }

    private void SpawnCharacter()
    {
        character = Instantiate(characterPrefab,Vector3.zero, spawnRotation);
        hasCharacter = true;
        character.controller = this;
        character.hasController = true;
    }

    public void Move(InputAction.CallbackContext context)
    {
        if(!hasCharacter) return;
        StickInput = context.ReadValue<Vector2>();
        
        //character.Move(StickInput);
    }
    
    public void Jump(InputAction.CallbackContext context)
    {
        if(!hasCharacter) return;
        
        if (context.started)
        {
            JumpPressed = true;
            character.Jump();
        }
        if(context.canceled) JumpPressed = false;
    }

    public void LightAttack(InputAction.CallbackContext context)
    {
        if(!hasCharacter) return;

        if (context.started)
        {
            LightAttackPressed = true;
            character.Attack(false);
        }
        if(context.canceled) LightAttackPressed = false;
    }

    public void HeavyAttack(InputAction.CallbackContext context)
    {
        if(!hasCharacter) return;

        if (context.started)
        {
            HeavyAttackPressed = true;
            character.Attack(true);
        }
        if(context.canceled) HeavyAttackPressed = false;
    }
    
    public void Dodge(InputAction.CallbackContext context)
    {
        if(context.started) DodgePressed = true;
        if(context.canceled) DodgePressed = false;
    }
}
