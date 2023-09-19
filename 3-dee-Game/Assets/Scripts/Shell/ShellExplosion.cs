﻿using Unity.Netcode;
using UnityEngine;

public class ShellExplosion : NetworkBehaviour
{
    public LayerMask m_TankMask;
    public GameObject m_particlesPrefab;

    private GameObject explosion;
              
    public float m_MaxDamage = 100f;                  
    public float m_ExplosionForce = 1000f;            
    public float m_MaxLifeTime = 2f;                  
    public float m_ExplosionRadius = 5f;


    public override void OnNetworkSpawn()
    {
        Destroy(gameObject, m_MaxLifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(!IsOwnedByServer) return;
        // Find all the tanks in an area around the shell and damage them.
        spawnExplosion_ServerRpc();
        //checkup_ClientRpc();
    }
    [ServerRpc(RequireOwnership =false)]
    private void spawnExplosion_ServerRpc()
    {
        explosion = Instantiate(m_particlesPrefab, transform.position, Quaternion.Euler(-90, 0, 0));
        explosion.GetComponent<NetworkObject>().Spawn();
        Destroy(gameObject);
    }
    [ClientRpc]
    private void checkup_ClientRpc()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, m_ExplosionRadius, m_TankMask);

        for (int i = 0; i < colliders.Length; i++)
        {
            Rigidbody target = colliders[i].GetComponent<Rigidbody>();
            if (!target)
                continue;
            target.AddExplosionForce(m_ExplosionForce, transform.position, m_ExplosionRadius);

            TankHealth targetHealth = target.GetComponent<TankHealth>();
            if (!targetHealth)
                continue;
            float damage = CalculateDamage(target.position);
            targetHealth.TakeDamage(damage);
        }
    }
    private float CalculateDamage(Vector3 targetPosition)
    {
        // Calculate the amount of damage a target should take based on it's position.
        Vector3 explosionToTarget = (targetPosition - transform.position);
        float explosionDistance = explosionToTarget.magnitude;
        float relativeDmg = (m_ExplosionRadius - explosionDistance) / (m_ExplosionRadius);
        float damage = m_MaxDamage * relativeDmg;
        return Mathf.Max(0f, damage);
    }
}