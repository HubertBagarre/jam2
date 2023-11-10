using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterController : MonoBehaviour
{
    
    [SerializeField] private Character characterPrefab;
    
    private Character character;

    private void Start()
    {
        character = Instantiate(characterPrefab);
    }

    public void Move(InputAction.CallbackContext context)
    {
        var input = context.ReadValue<Vector2>();
        
        character.Move(input);
    }
}
