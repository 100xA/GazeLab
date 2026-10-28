// Author: Jan Biernacki. Matriculation number: TO BE PROVIDED.
using UnityEngine;

public class RockController : MonoBehaviour
{
    private GameController game;
    private void Start() { game = FindObjectOfType<GameController>(); }
    private void Update()
    {
        if (game != null && (game.GameOver || game.IsPaused)) return;
        transform.Rotate(0f, 0f, -15f * Time.deltaTime, Space.Self);
        transform.Translate(-3f * Time.deltaTime, 0f, 0f, Space.World);
        if (transform.position.x < -16f) Destroy(gameObject);
    }
}
