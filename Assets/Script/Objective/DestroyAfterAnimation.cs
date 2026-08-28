using UnityEngine;

[RequireComponent(typeof(Animator))]
public class DestroyAfterAnimation : MonoBehaviour
{
    private void Start()
    {
        Animator anim = GetComponent<Animator>();
        // Menghancurkan GameObject ini secara otomatis setelah durasi animasi state pertama selesai
        Destroy(gameObject, anim.GetCurrentAnimatorStateInfo(0).length);
    }
}