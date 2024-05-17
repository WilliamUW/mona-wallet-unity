using Monaverse.UI;
using UnityEngine;

public class TestModal : MonoBehaviour
{
    void Start()
    {
        Invoke("EnableModal", 0.1f);
        //Invoke("CloseModal", 10.0f);
    }

    private void EnableModal()
    {
        MonaverseModal.OpenModal();
    }

    private void CloseModal()
    {
        MonaverseModal.CloseModal();
    }
}
