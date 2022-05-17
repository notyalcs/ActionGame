using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Comp_PlayerCombatController : MonoBehaviour
{

    [Header("Attack Info")]
    [SerializeField] private float _attackRate = 2.0f;

    private float _nextAttackTime = 0.0f;
    private Animator _animator;

    private void Start() {
        _animator = GetComponent<Animator>();
    }

    private void Update() {
        if (_animator.GetBool("Attack2") || _animator.GetBool("Attack3")) { return; }

        if (Time.time >= _nextAttackTime) {
            if (Input.GetButtonDown("Fire1")) {
                Attack();
                _nextAttackTime = Time.time + 1.0f / _attackRate;
            }
        }
    }

    private void Attack() {
        _animator.SetTrigger("Attack");
    }

}
