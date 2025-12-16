using System.Collections;
using UnityEngine;

public class AnimationTrigger : MonoBehaviour
{
    [SerializeField] private Animation animation;

    private void Start()
    {
        StartCoroutine(Check());
    }

    private IEnumerator Check()
    {
        yield return new WaitForSeconds(3f);
        animation.Play();
    }
}
