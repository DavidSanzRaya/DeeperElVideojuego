
using System;
using UnityEngine;
using UnityEngine.Events;

public class GoDeeper : MonoBehaviour
{
    [SerializeField] 
    private LayersManager layerManger;

    public static event Action OnDeeper;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Player player;
        if (player = collision.GetComponent<Player>())
        {
            player.OnGoDeeper();
            layerManger.NextLevel();
            OnDeeper?.Invoke();
        }
    }
}