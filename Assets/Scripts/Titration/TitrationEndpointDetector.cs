using System;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class TitrationEndpointDetector : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    [Header("Reaction setup")]
    [SerializeField, Min(0.000001f)] private float standardAcidConcentrationMolPerL = 0.1f;
    [SerializeField, Min(0.000001f)] private float initialNaohVolumeMl = 10f;
    [SerializeField, Min(0.000001f)] private float nominalNaohConcentrationMolPerL = 0.1f;
    [Tooltip("Reactive H+ equivalents per mole of acid. Use 1 for HCl.")]
    [SerializeField, Min(1f)] private float acidEquivalentFactor = 1f;
    [Tooltip("Reactive OH- equivalents per mole of base. Use 1 for NaOH.")]
    [SerializeField, Min(1f)] private float baseEquivalentFactor = 1f;

    [Header("Indicator")]
    [SerializeField] private Renderer liquidRenderer;
    [SerializeField] private Color baseExcessPink = new Color(1f, 0.25f, 0.55f, 0.65f);
    [SerializeField] private Color endpointColorless = new Color(1f, 1f, 1f, 0.12f);

    [Header("Endpoint output")]
    [SerializeField] private UnityEvent onEndpointReached = new UnityEvent();

    private MaterialPropertyBlock propertyBlock;
    private bool endpointReached;
    private float acidVolumeAddedMl;
    private float finalAcidVolumeMl;

    public event Action EndpointReachedEvent;

    public bool IsEndpointReached => endpointReached;
    public float AcidVolumeAddedMl => acidVolumeAddedMl;
    public float FinalAcidVolumeMl => finalAcidVolumeMl;

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
        ResetTitration();
    }

    /// <summary>
    /// Receives the total acid volume delivered by the burette, in millilitres.
    /// Returns true once equivalence has been reached and remains true thereafter.
    /// </summary>
    public bool CheckEndpoint(float totalAcidVolumeAddedMl)
    {
        if (endpointReached)
        {
            return true;
        }

        acidVolumeAddedMl = Mathf.Max(0f, totalAcidVolumeAddedMl);

        double acidEquivalents =
            standardAcidConcentrationMolPerL * (acidVolumeAddedMl / 1000.0) * acidEquivalentFactor;
        double initialBaseEquivalents =
            nominalNaohConcentrationMolPerL * (initialNaohVolumeMl / 1000.0) * baseEquivalentFactor;

        if (acidEquivalents < initialBaseEquivalents)
        {
            return false;
        }

        endpointReached = true;
        finalAcidVolumeMl = acidVolumeAddedMl;
        ApplyLiquidColor(endpointColorless);

        EndpointReachedEvent?.Invoke();
        onEndpointReached?.Invoke();
        return true;
    }

    /// <summary>
    /// Compatibility method for a Burette module that polls after each drop.
    /// </summary>
    public bool EndpointReached()
    {
        return endpointReached;
    }

    public float GetFinalAcidVolume()
    {
        return endpointReached ? finalAcidVolumeMl : 0f;
    }

    /// <summary>
    /// Calculates standardized NaOH concentration from the locked endpoint volume.
    /// Returns zero until the endpoint has been reached.
    /// </summary>
    public float CalculateStandardizedNaohConcentration()
    {
        if (!endpointReached || initialNaohVolumeMl <= 0f)
        {
            return 0f;
        }

        return standardAcidConcentrationMolPerL
            * finalAcidVolumeMl
            * acidEquivalentFactor
            / (initialNaohVolumeMl * baseEquivalentFactor);
    }

    public void ResetTitration()
    {
        endpointReached = false;
        acidVolumeAddedMl = 0f;
        finalAcidVolumeMl = 0f;
        ApplyLiquidColor(baseExcessPink);
    }

    private void ApplyLiquidColor(Color color)
    {
        if (liquidRenderer == null || liquidRenderer.sharedMaterial == null)
        {
            return;
        }

        propertyBlock ??= new MaterialPropertyBlock();
        liquidRenderer.GetPropertyBlock(propertyBlock);

        Material material = liquidRenderer.sharedMaterial;
        if (material.HasProperty(BaseColorId))
        {
            propertyBlock.SetColor(BaseColorId, color);
        }
        else if (material.HasProperty(ColorId))
        {
            propertyBlock.SetColor(ColorId, color);
        }
        else
        {
            Debug.LogWarning(
                "The assigned liquid material has no _BaseColor or _Color property.",
                this);
            return;
        }

        liquidRenderer.SetPropertyBlock(propertyBlock);
    }

    private void OnValidate()
    {
        standardAcidConcentrationMolPerL = Mathf.Max(0.000001f, standardAcidConcentrationMolPerL);
        initialNaohVolumeMl = Mathf.Max(0.000001f, initialNaohVolumeMl);
        nominalNaohConcentrationMolPerL = Mathf.Max(0.000001f, nominalNaohConcentrationMolPerL);
        acidEquivalentFactor = Mathf.Max(1f, acidEquivalentFactor);
        baseEquivalentFactor = Mathf.Max(1f, baseEquivalentFactor);
    }
}
