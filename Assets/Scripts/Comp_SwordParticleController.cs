using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Comp_SwordParticleController : MonoBehaviour
{

    [Header("Spawn Info")]
    [SerializeField] private GameObject _trailPrefab;
    [SerializeField] private Transform _spawnTransform;

    public void StartTrail() {
        GameObject trailObj = Instantiate(_trailPrefab, _spawnTransform);
    }

}
