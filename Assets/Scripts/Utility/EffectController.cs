using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

public class EffectController : MonoBehaviour
{
    [SerializeField]private AudioSource audioSource;
    public float delayTime = 0.5f;
    void OnEnable()
    {
        audioSource.PlayDelayed(delayTime);
    }
}

