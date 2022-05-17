using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Comp_CharacterController : MonoBehaviour
{
    [Header("External Allow Move")]
    [SerializeField] public bool _canMove = true;

    [Header("Speed (Strafing, Normal, Sprinting")]
    [SerializeField] private Vector3 _moveSpeed = Vector3.zero;
    [SerializeField] private float _attackMovementLimiter = 0.2f;

    [Header("Sharpness")]
    [SerializeField] private float _rotationSharpness = 10.0f;
    [SerializeField] private float _moveSharpness = 10.0f;

    private Animator _animator;
    private Comp_CameraController _cameraController;

    private float _strafeSpeed;
    private float _normalSpeed;
    private float _sprintSpeed;
    private LayerMask _layerMask;
    private Collider[] _obstructions = new Collider[8];

    private bool _strafing;
    private bool _sprinting;
    private float _strafeParameter;
    private Vector3 _strafeParameterXZ;

    private float _targetSpeed;
    private Quaternion _targetRotation;

    private float _newSpeed;
    private Vector3 _newVelocity;
    private Quaternion _newRotation;

    private void Start() {
        _animator = GetComponent<Animator>();
        _cameraController = GetComponent<Comp_CameraController>();

        _strafeSpeed = _moveSpeed.x;
        _normalSpeed = _moveSpeed.y;
        _sprintSpeed = _moveSpeed.z;

        int mask = 0;
        for (int i = 0; i < 32; ++i) {
            if (!Physics.GetIgnoreLayerCollision(gameObject.layer, i)) {
                mask |= 1 << i;
            }
        }
        _layerMask = mask;

        _animator.applyRootMotion = false;
    }

    private void Update() {
        Vector3 moveInputVector = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        Vector3 cameraPlanarDirection = _cameraController.CameraPlanarDirection;
        Quaternion cameraPlanarRotation = Quaternion.LookRotation(cameraPlanarDirection);

        Vector3 moveInputVectorOriented = cameraPlanarRotation * moveInputVector.normalized;

        _strafing = _cameraController.LockedOn;
        if (_strafing) {
            _sprinting = Input.GetButtonDown("Sprint") && (moveInputVector != Vector3.zero);
        } else {
            _sprinting = Input.GetButton("Sprint") && (moveInputVector != Vector3.zero);
        }

        if (_sprinting) {
            _cameraController.ToggleLockOn(false);
        }

        // Move speed
        if (_sprinting) { _targetSpeed = (moveInputVector != Vector3.zero) ? _sprintSpeed : 0; }
        else if (_strafing) { _targetSpeed = (moveInputVector != Vector3.zero) ? _strafeSpeed : 0; }
        else { _targetSpeed = (moveInputVector != Vector3.zero) ? _normalSpeed : 0; }
        _newSpeed = Mathf.Lerp(_newSpeed, _targetSpeed, Time.deltaTime * _moveSharpness);

        // Velocity
        _newVelocity = moveInputVectorOriented * _newSpeed;
        if (!_canMove) { _newVelocity *= _attackMovementLimiter; }
        transform.Translate(_newVelocity * Time.deltaTime, Space.World);

        // Rotation
        if (_strafing) {
            Vector3 toTarget = _cameraController.Target.TargetTransform.position - transform.position;
            Vector3 planarToTarget = Vector3.ProjectOnPlane(toTarget, Vector3.up);

            _targetRotation = Quaternion.LookRotation(planarToTarget);
            _newRotation = Quaternion.Slerp(transform.rotation, _targetRotation, Time.deltaTime * _rotationSharpness);
            transform.rotation = _newRotation;
        } else if (_targetSpeed != 0) {
            _targetRotation = Quaternion.LookRotation(moveInputVectorOriented);
            _newRotation = Quaternion.Slerp(transform.rotation, _targetRotation, Time.deltaTime * _rotationSharpness);
            transform.rotation = _newRotation;
        }

        if (!_canMove) { return; }

        // Animation
        if (_strafing) {
            _strafeParameter = Mathf.Clamp01(_strafeParameter + Time.deltaTime * 4);
            _strafeParameterXZ = Vector3.Lerp(_strafeParameterXZ, moveInputVector * _newSpeed, Time.deltaTime * _moveSharpness);
        } else {
            _strafeParameter = Mathf.Clamp01(_strafeParameter - Time.deltaTime * 4);
            _strafeParameterXZ = Vector3.Lerp(_strafeParameterXZ, Vector3.forward * _newSpeed, Time.deltaTime * _moveSharpness);
        }

        _animator.SetFloat("Strafing", _strafeParameter);
        _animator.SetFloat("StrafingX", Mathf.Round(_strafeParameterXZ.x * 100.0f) / 100.0f);
        _animator.SetFloat("StrafingZ", Mathf.Round(_strafeParameterXZ.z * 100.0f) / 100.0f);

        // Lock on toggle
        if (Input.GetButtonDown("Lock On")) {
            _cameraController.ToggleLockOn(!_cameraController.LockedOn);
        }
    }

}
