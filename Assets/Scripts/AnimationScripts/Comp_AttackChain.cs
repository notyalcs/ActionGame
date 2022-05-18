using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Comp_AttackChain : StateMachineBehaviour
{

    [Header("External Links")]
    [SerializeField] private Comp_CharacterController _characterController = null;

    [Header("Movement")]
    [SerializeField] private float _moveDistance = 0.8f;
    [SerializeField] private float _moveSpeed = 15f;

    private Vector3 _startPosition;
    private Vector3 _endPosition;
    private bool _chain = false;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {

        if (_characterController == null) {
            _characterController = animator.GetComponent<Comp_CharacterController>();
        }

        _characterController._canMove = false;
        _chain = false;

        animator.SetFloat("StrafingX", 0);
        animator.SetFloat("StrafingZ", 0);

        animator.SetBool("Attack2", false);
        animator.SetBool("Attack3", false);

        _startPosition = _characterController.gameObject.transform.position;
        _endPosition = _startPosition + _characterController.gameObject.transform.forward * _moveDistance;
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        if (Input.GetButtonDown("Fire1")) {
            if (stateInfo.normalizedTime < 0.6f) {
                animator.SetBool("Attack2", true);
                _chain = true;
            }
        }
        _startPosition = Vector3.Lerp(_startPosition, _endPosition, _moveSpeed * Time.deltaTime);
        _characterController.gameObject.transform.position = _startPosition;
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        if (_chain) { return; }

        _characterController._canMove = true;
        animator.SetBool("Attack2", false);
        animator.SetBool("Attack3", false);
    }

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    //override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    //override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateMove is called right after Animator.OnAnimatorMove()
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that processes and affects root motion
    //}

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}
}
