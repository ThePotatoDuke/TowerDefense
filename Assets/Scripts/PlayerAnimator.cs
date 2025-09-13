using System;
using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    private const string PLAYER_WALKING = "isWalking";
    private Animator animator;
    [SerializeField] private Player player;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        player.OnStateChanged += Player_OnstateChanged;
    }

    private void Player_OnstateChanged(Player.PlayerState state)
    {
        switch (state)
        {
            case Player.PlayerState.Idle:
                animator.SetBool(PLAYER_WALKING, false);
                break;
            case Player.PlayerState.Walking:
                animator.SetBool(PLAYER_WALKING, true);
                break;
        }
    }
}
