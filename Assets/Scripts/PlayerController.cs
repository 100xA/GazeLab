// Author: Jan Biernacki. Matriculation number: TO BE PROVIDED.
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private GameController game;
    private void Start() { game = FindObjectOfType<GameController>(); }
    private void OnCollisionEnter2D(Collision2D other)
    {
        if (game == null || game.GameOver || game.IsPaused) return;
        bool isRock = other.gameObject.GetComponent<RockController>() != null;
        bool isSatellite = other.gameObject.GetComponent<SatelliteController>() != null;
        if (!isRock && !isSatellite) return;
        if (isRock) game.damage = Mathf.Min(4, game.damage + 1);
        foreach (Collider2D collider in other.gameObject.GetComponentsInChildren<Collider2D>()) collider.enabled = false;
        Destroy(other.gameObject);
    }
}
