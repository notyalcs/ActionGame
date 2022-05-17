using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Comp_CameraController : MonoBehaviour
{

    [Header("Framing")]
    [SerializeField] private Camera _camera = null;
    [SerializeField] private Transform _followTransform = null;
    [SerializeField] private Vector3 _framingNormal = Vector3.zero;

    [Header("Distance")]
    [SerializeField] private float _zoomSpeed = 10.0f;
    [SerializeField] private float _defaultDistance = 5.0f;
    [SerializeField] private float _minDistance = 0.1f;
    [SerializeField] private float _maxDistance = 10.0f;

    [Header("Rotation")]
    [SerializeField] private bool _invertX = false;
    [SerializeField] private bool _invertY = false;
    [SerializeField] private float _rotationSharpness = 25.0f;
    [SerializeField] private float _defaultVerticalAngle = 20.0f;
    [SerializeField] [Range(-90, 90)] private float _minVerticalAngle = -90.0f;
    [SerializeField] [Range(-90, 90)] private float _maxVerticalAngle = 90.0f;

    [Header("Obstructions")]
    [SerializeField] private float _checkRadius = 0.2f;
    [SerializeField] private LayerMask _obstructionLayers = -1;
    private List<Collider> _ignoreColliders = new List<Collider>();

    [Header("Lock On")]
    [SerializeField] private float _lockOnLossTime = 1.0f;
    [SerializeField] private float _lockOnDistance = 15.0f;
    [SerializeField] private LayerMask _lockOnLayers = -1;
    [SerializeField] private Vector3 _lockOnFraming = new Vector3(0.25f, 0.25f, 0);
    [SerializeField] [Range(1, 179)] private float _lockOnFOV = 40.0f;

    public bool LockedOn { get => _lockedOn; }
    public ITargetable Target { get => _target; }
    public Vector3 CameraPlanarDirection { get => _planarDirection; }

    private float _fovNormal;
    private float _framingLerp;
    private Vector3 _planarDirection;
    private float _targetDistance;
    private Vector3 _targetPosition;
    private Quaternion _targetRotation;
    private float _targetVerticalAngle;

    private Vector3 _newPosition;
    private Quaternion _newRotation;

    private bool _lockedOn;
    private float _lockOnLossTimeCurrent;
    private ITargetable _target;

    private void OnValidate() {
        _defaultDistance = Mathf.Clamp(_defaultDistance, _minDistance, _maxDistance);
        _defaultVerticalAngle = Mathf.Clamp(_defaultVerticalAngle, _minVerticalAngle, _maxVerticalAngle);
    }

    private void Start() {
        _ignoreColliders.AddRange(GetComponentsInChildren<Collider>());

        _fovNormal = _camera.fieldOfView;
        _planarDirection = _followTransform.forward;

        _targetDistance = _defaultDistance;
        _targetVerticalAngle = _defaultVerticalAngle;
        _targetRotation = Quaternion.LookRotation(_planarDirection) * Quaternion.Euler(_targetVerticalAngle, 0, 0);
        _targetPosition = _followTransform.position - (_targetRotation * Vector3.forward) * _targetDistance;

        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update() {
        if (Cursor.lockState != CursorLockMode.Locked) { return; }

        // Handle Inputs
        float zoom = Input.GetAxis("Mouse ScrollWheel") * -1.0f * _zoomSpeed;
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        if (_invertX) { mouseX = -mouseX; }
        if (_invertY) { mouseY = -mouseY; }

        // Framing
        Vector3 framing = Vector3.Lerp(_framingNormal, _lockOnFraming, _framingLerp);
        Vector3 focusPosition = _followTransform.position + _followTransform.TransformDirection(framing);
        float fov = Mathf.Lerp(_fovNormal, _lockOnFOV, _framingLerp);
        _camera.fieldOfView = fov;

        if (_lockedOn && _target != null) {
            Vector3 camToTarget = _target.TargetTransform.position - _camera.transform.position;
            Vector3 planarCamToTarget = Vector3.ProjectOnPlane(camToTarget, Vector3.up);
            Quaternion lookRotation = Quaternion.LookRotation(camToTarget, Vector3.up);

            _framingLerp = Mathf.Clamp01(_framingLerp + Time.deltaTime * 4);
            _planarDirection = (planarCamToTarget != Vector3.zero) ? planarCamToTarget.normalized : _planarDirection;
            _targetDistance = Mathf.Clamp(_targetDistance + zoom, _minDistance, _maxDistance);
            _targetVerticalAngle = Mathf.Clamp(lookRotation.eulerAngles.x, _minVerticalAngle, _maxVerticalAngle);
        } else {
            _framingLerp = Mathf.Clamp01(_framingLerp - Time.deltaTime * 4);
            _planarDirection = Quaternion.Euler(0, mouseX, 0) * _planarDirection;
            _targetDistance = Mathf.Clamp(_targetDistance + zoom, _minDistance, _maxDistance);
            _targetVerticalAngle = Mathf.Clamp(_targetVerticalAngle + mouseY, _minVerticalAngle, _maxVerticalAngle);
        }

        Debug.DrawLine(_camera.transform.position, _camera.transform.position + _planarDirection, Color.red);

        // Handle Obstructions
        float smallestDistance = _targetDistance;
        RaycastHit[] hits = Physics.SphereCastAll(focusPosition, _checkRadius, _targetRotation * -Vector3.forward, _targetDistance, _obstructionLayers);

        if (hits.Length > 0) {
            foreach (RaycastHit hit in hits) {
                if (!_ignoreColliders.Contains(hit.collider)) {
                    smallestDistance = (hit.distance < smallestDistance) ? hit.distance : smallestDistance;
                }
            }
        }

        // Final Targets
        _targetRotation = Quaternion.LookRotation(_planarDirection) * Quaternion.Euler(_targetVerticalAngle, 0, 0);
        _targetPosition = focusPosition - (_targetRotation * Vector3.forward) * smallestDistance;

        // Handle Smoothing
        _newRotation = Quaternion.Slerp(_camera.transform.rotation, _targetRotation, Time.deltaTime * _rotationSharpness);
        _newPosition = Vector3.Lerp(_camera.transform.position, _targetPosition, Time.deltaTime * _rotationSharpness);

        // Apply Changes
        _camera.transform.rotation = _newRotation;
        _camera.transform.position = _newPosition;

        if (_lockedOn && _target != null) {
            bool valid = _target.Targetable && InDistance(_target) && InScreen(_target) && NotBlocked(_target);

            if (valid) {
                _lockOnLossTimeCurrent = 0;
            } else {
                _lockOnLossTimeCurrent = Mathf.Clamp(_lockOnLossTimeCurrent + Time.deltaTime, 0, _lockOnLossTime);
            }

            if (_lockOnLossTimeCurrent >= _lockOnLossTime) {
                _lockedOn = false;
            }
        }
    }

    private void OnDrawGizmos() {
        if (_lockedOn) {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _lockOnDistance);
        }
    }

    public void ToggleLockOn(bool toggle) {
        if (toggle == _lockedOn) { return; }

        // Toggle
        _lockedOn = !_lockedOn;

        // Find a lock on target
        if (_lockedOn) {
            // Filter targetables
            List<ITargetable> targetables = new List<ITargetable>();
            Collider[] colliders = Physics.OverlapSphere(transform.position, _lockOnDistance, _lockOnLayers);
            foreach (Collider collider in colliders) {
                ITargetable targetable = collider.GetComponent<ITargetable>();
                if (targetable != null) {
                    if (targetable.Targetable) {
                        if (InScreen(targetable)) {
                            if (NotBlocked(targetable)) {
                                targetables.Add(targetable);
                            }
                        }
                    }
                }
            }

            // Find closest to center of screen
            float hypotenuse;
            float smallestHypotenuse = Mathf.Infinity;
            ITargetable closestTargetable = null;
            foreach (ITargetable targetable in targetables) {
                hypotenuse = CalculateHypotenuse(targetable.TargetTransform.position);
                if (smallestHypotenuse > hypotenuse) {
                    closestTargetable = targetable;
                    smallestHypotenuse = hypotenuse;
                }
            }

            // Apply
            _target = closestTargetable;
            _lockedOn = (closestTargetable != null);
        }
    }

    private bool InDistance(ITargetable targetable) {
        float distance = Vector3.Distance(transform.position, targetable.TargetTransform.position);
        return distance <= _lockOnDistance;
    }

    private bool InScreen(ITargetable targetable) {
        Vector3 viewPortPosition = _camera.WorldToViewportPoint(targetable.TargetTransform.position);

        if (!(viewPortPosition.x > 0) || !(viewPortPosition.x < 1)) { return false; }
        if (!(viewPortPosition.y > 0) || !(viewPortPosition.y < 1)) { return false; }
        if (!(viewPortPosition.z > 0)) { return false; }

        return true;
    }

    private bool NotBlocked(ITargetable targetable) {
        Vector3 origin = _camera.transform.position;
        Vector3 direction = targetable.TargetTransform.position - origin;

        float radius = 0.15f;
        float distance = direction.magnitude;

        return !Physics.SphereCast(origin, radius, direction, out RaycastHit hit, distance, _obstructionLayers);
    }

    private float CalculateHypotenuse(Vector3 position) {
        float screenCenterX = _camera.pixelWidth / 2;
        float screenCenterY = _camera.pixelHeight / 2;

        Vector3 screenPosition = _camera.WorldToScreenPoint(position);
        float xDelta = screenCenterX - screenPosition.x;
        float yDelta = screenCenterY - screenPosition.y;

        return Mathf.Pow(xDelta, 2) + Mathf.Pow(yDelta, 2);
    }

}
