using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class MenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Navigation Settings")]
#if UNITY_EDITOR
    [Tooltip("ลากไฟล์ Scene จากหน้าต่าง Project มาใส่ตรงนี้ได้เลย")]
    public UnityEditor.SceneAsset sceneAsset;
#endif

    [HideInInspector]
    public string targetSceneName;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (sceneAsset != null)
            targetSceneName = sceneAsset.name;
    }
#endif

    [Header("Hover Settings")]
    [Tooltip("ลาก GameObject ที่ต้องการเปิด/ปิดตอนเอาเมาส์ชี้ (ถ้าไม่มี ปล่อยว่างไว้ได้เลย)")]
    public GameObject hoverObject;

    [Tooltip("ลากคอมโพเนนต์ Animator (เช่น Motion Titles Pack) มาใส่ตรงนี้")]
    public Animator targetAnimator;

    [Tooltip("ชื่อ Trigger Parameter ใน Animator ตอนเมาส์เข้า (ที่สร้างไว้ในแท็บ Parameters)")]
    public string hoverInTrigger = "In";

    // จะทำงานเมื่อนำเมาส์มาวางบนปุ่ม → เล่นแอนิเมชันครั้งเดียว
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverObject != null)
            hoverObject.SetActive(true);

        if (targetAnimator != null && targetAnimator.runtimeAnimatorController != null)
            targetAnimator.SetTrigger(hoverInTrigger); // เรียก Trigger → เล่น In แค่ครั้งเดียว
    }

    // จะทำงานเมื่อนำเมาส์ออกจากปุ่ม
    public void OnPointerExit(PointerEventData eventData)
    {
        // ไม่เล่น Out — แค่ค้างอยู่ที่เฟรมสุดท้ายของ In
        if (hoverObject != null)
            hoverObject.SetActive(false);
    }

    // คลิกเพื่อเปลี่ยนหน้า
    public void OnPointerClick(PointerEventData eventData)
    {
        GoToPage();
    }

    public void GoToPage()
    {
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            Debug.Log("เปลี่ยนหน้าไปยัง: " + targetSceneName);
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogWarning("ยังไม่ได้ลาก Scene ใส่ในช่อง Scene Asset ครับ!");
        }
    }
}
