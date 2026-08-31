using TMPro; // 파티 편성 상태 문구 사용
using UnityEngine; // 캐릭터 초상화와 컴포넌트 사용
using UnityEngine.UI; // 선택 버튼과 이미지 사용

public sealed class PartyDeploymentMemberButton : MonoBehaviour // 파티 편성 캐릭터 선택 버튼
{
    [Header("편성 UI")]
    [SerializeField] private Button selectButton; // 캐릭터 선택 버튼
    [SerializeField] private Image portraitImage; // 캐릭터 초상화
    [SerializeField] private TMP_Text nameText; // 캐릭터 이름 문구
    [SerializeField] private TMP_Text statusText; // 출전 상태 문구

    private CharacterData characterData; // 현재 표시 캐릭터
    private CharacterRecoveryManager recoveryManager; // 회복 상태 관리자
    private BattleResultManager resultManager; // HP 사망 상태 관리자

    public CharacterData Character => characterData; // 현재 캐릭터 조회
    public PartyMemberDeploymentViewState ViewState { get; private set; } // 현재 UI 상태 조회
    public bool CanSelect => ViewState.CanSelect; // 현재 선택 가능 여부 조회

    public void Bind(CharacterData targetCharacter) // 캐릭터 선택 버튼 연결
    {
        characterData = targetCharacter; // 표시 캐릭터 저장
        Refresh(); // 최초 상태 갱신
    }

    public void Refresh() // 캐릭터 출전 상태 화면 갱신
    {
        EnsureManagers(); // 상태 관리자 준비

        ViewState =
            PartyDeploymentViewStateFactory.Create(characterData); // 현재 출전 표시 상태 계산

        if (selectButton != null)
        {
            selectButton.interactable = ViewState.CanSelect; // 사망·회복 중 버튼 선택 차단
        }

        if (portraitImage != null)
        {
            portraitImage.sprite =
                characterData != null
                    ? characterData.Portrait
                    : null; // 캐릭터 초상화 적용

            portraitImage.color =
                ViewState.IsDimmed
                    ? new Color(0.45f, 0.45f, 0.45f, 0.75f)
                    : Color.white; // 출전 불가 초상화 흐림 적용
        }

        if (nameText != null)
        {
            nameText.text =
                characterData != null
                    ? characterData.DisplayName
                    : "미지정"; // 캐릭터 이름 적용
        }

        if (statusText != null)
        {
            statusText.text = ViewState.StatusText; // 출전 상태 문구 적용
        }
    }

    private void OnEnable() // 버튼 활성화 시 상태 이벤트 연결
    {
        EnsureManagers(); // 상태 관리자 준비
        SubscribeStateEvents(); // 파티와 회복 상태 변경 연결
        Refresh(); // 활성화 상태 즉시 갱신
    }

    private void OnDisable() // 버튼 비활성화 시 상태 이벤트 해제
    {
        UnsubscribeStateEvents(); // 파티와 회복 상태 변경 해제
    }

    private void EnsureManagers() // 출전 상태 관리자 준비
    {
        recoveryManager =
            recoveryManager != null
                ? recoveryManager
                : CharacterRecoveryManager.EnsureInstance(); // 회복 관리자 준비

        resultManager =
            resultManager != null
                ? resultManager
                : BattleResultManager.EnsureInstance(); // 저장 파티 상태 관리자 준비
    }

    private void SubscribeStateEvents() // 상태 변경 이벤트 연결
    {
        UnsubscribeStateEvents(); // 중복 이벤트 연결 방지

        if (recoveryManager != null)
        {
            recoveryManager.RecoveryStateChanged += Refresh; // 회복 진행 상태 변경 연결
        }

        if (resultManager != null)
        {
            resultManager.PartyStateChanged += Refresh; // HP·정신력·사망 상태 변경 연결
        }
    }

    private void UnsubscribeStateEvents() // 상태 변경 이벤트 해제
    {
        if (recoveryManager != null)
        {
            recoveryManager.RecoveryStateChanged -= Refresh; // 회복 상태 이벤트 해제
        }

        if (resultManager != null)
        {
            resultManager.PartyStateChanged -= Refresh; // 파티 상태 이벤트 해제
        }
    }
}
