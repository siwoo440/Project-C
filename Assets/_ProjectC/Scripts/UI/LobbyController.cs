using UnityEngine;

public sealed class LobbyController : MonoBehaviour
{
    private void Start() // 거점 화면 시작
    { // 시작 처리 시작
        Canvas canvas = FindFirstObjectByType<Canvas>(); // 거점 캔버스 조회
        CharacterRosterData roster = Resources.Load<CharacterRosterData>("Characters/ArcanaRoster"); // 공용 캐릭터 목록 조회
        if (canvas == null || roster == null) // 화면 또는 목록 누락 확인
        { // 누락 처리 시작
            Debug.LogError("[LobbyController] 편성 화면에 필요한 캔버스 또는 캐릭터 목록이 없습니다."); // 데이터 누락 안내
            return; // 편성 화면 생성 중단
        } // 누락 처리 종료
        Texture2D background = Resources.Load<Texture2D>("UI/LobbyBackground"); // 거점 배경 이미지 조회
        if (background != null) // 배경 이미지 존재 확인
        { // 배경 연결 시작
            LobbyBackgroundInstaller.Install(canvas, background); // 거점 배경 표시
        } // 배경 연결 종료
        CharacterRecoveryManager recovery = CharacterRecoveryManager.EnsureInstance(); // 회복 상태 관리자 준비
        BattleResultManager.EnsureInstance(); // 파티 상태 관리자 준비
        LobbyAutosaveController autosave = gameObject.AddComponent<LobbyAutosaveController>(); // 거점 자동 저장 관리자 추가
        autosave.Initialize(LobbySaveService.DefaultPath); // 기본 저장 경로 연결
        GameObject viewObject = new GameObject("PartyRosterView", typeof(RectTransform), typeof(PartyRosterView)); // 편성 화면 객체 생성
        viewObject.transform.SetParent(canvas.transform, false); // 거점 캔버스 연결
        viewObject.GetComponent<PartyRosterView>().Initialize(roster, new CharacterRecoveryDeploymentStatusProvider(recovery)); // 캐릭터 목록 화면 구성
    } // 시작 처리 종료
    public void OpenSettings()
    {
        if (SceneFlowManager.Instance == null) // SceneFlowManager 존재 여부 확인
        {
            Debug.LogError("[LobbyController] SceneFlowManager를 찾을 수 없습니다."); // 관리자 누락 출력
            return; // 설정 이동 중단
        }

        SceneFlowManager.Instance.LoadScene("90_Settings"); // 설정 씬 이동
    }
}
