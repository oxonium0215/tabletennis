using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.Events;
using System;
namespace StepUpTableTennis
{

public class CountdownTimer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI countdownText;
    
    [Header("Timer Settings")]
    [SerializeField] private float startValue = 3f;
    [SerializeField] private bool countDecimal = false;
    [SerializeField] private string countdownFormat = "0";
    
    [Header("Animation Settings")]
    [SerializeField] private bool useAnimation = true;
    [SerializeField] private float scaleAnimationDuration = 0.5f;
    [SerializeField] private float scalePunchPower = 0.5f;
    [SerializeField] private int scalePunchVibrato = 1;
    [SerializeField] private float fadeOutDuration = 0.3f;
    
    [Header("Events")]
    public UnityEvent onCountdownStart;
    public UnityEvent onCountdownTick;
    public UnityEvent onCountdownComplete;
    
    private float currentValue;
    private bool isRunning;
    private Sequence currentAnimation;
    
    private void Start()
    {
        if (countdownText == null)
        {
            countdownText = GetComponent<TextMeshProUGUI>();
        }
        
        ResetText();
    }
    
    public void StartCountdown()
    {
        if (isRunning) return;
        
        isRunning = true;
        currentValue = startValue;
        onCountdownStart?.Invoke();
        UpdateText();
    }
    
    private void Update()
    {
        // スペースキーを押したらカウントダウンを開始
        if (Input.GetKeyDown(KeyCode.Space) || OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.LTouch))
        {
            StartCountdown();
        }
        if (!isRunning) return;
        
        currentValue -= Time.deltaTime;
        
        if (currentValue <= 0)
        {
            CompleteCountdown();
            return;
        }
        
        UpdateText();
    }
    
    private void UpdateText()
    {
        string format = countDecimal ? "F1" : countdownFormat;
        float displayValue = countDecimal ? currentValue : Mathf.Ceil(currentValue);
        countdownText.text = displayValue.ToString(format);
        
        if (useAnimation)
        {
            AnimateText();
        }
        
        onCountdownTick?.Invoke();
    }
    
    private void AnimateText()
    {
        currentAnimation?.Kill();
        
        currentAnimation = DOTween.Sequence()
            .Join(countdownText.transform.DOPunchScale(
                Vector3.one * scalePunchPower,
                scaleAnimationDuration,
                scalePunchVibrato,
                0.5f
            ));
    }
    
    private void CompleteCountdown()
    {
        isRunning = false;
        currentValue = 0;
        UpdateText();
        
        if (useAnimation)
        {
            currentAnimation?.Kill();
            countdownText.transform.DOScale(Vector3.one, 0.1f);
            countdownText.DOFade(0, fadeOutDuration)
                .OnComplete(() => {
                    ResetText();
                    onCountdownComplete?.Invoke();
                });
        }
        else
        {
            ResetText();
            onCountdownComplete?.Invoke();
        }
    }
    
    private void ResetText()
    {
        countdownText.alpha = 1f;
        countdownText.transform.localScale = Vector3.one;
        countdownText.text = string.Empty;
    }
    
    public void StopCountdown()
    {
        isRunning = false;
        currentAnimation?.Kill();
        ResetText();
    }
    
    private void OnDestroy()
    {
        currentAnimation?.Kill();
    }
}
}