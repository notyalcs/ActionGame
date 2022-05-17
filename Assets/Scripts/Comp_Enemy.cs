using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Comp_Enemy : MonoBehaviour, ITargetable
{

    [SerializeField] private bool _targetable = true;
    [SerializeField] private Transform _targetTransform;

    bool ITargetable.Targetable { get => _targetable; }
    Transform ITargetable.TargetTransform { get => _targetTransform; }

}
