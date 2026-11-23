using System; // 설비 열거형 목록 사용
using UnityEngine; // IMGUI와 화면 기능 사용
using UnityEngine.InputSystem; // F7 입력 확인
using UnityEngine.SceneManagement; // 현재 Scene 이름 확인

public sealed class FacilityUpgradeDebugView : MonoBehaviour // 35일차 임시 설비 강화 화면
{
    private const string LobbySceneName = "20_Lobby"; // 강화 테스트 허용 로비 Scene 이름
    private FacilityType selectedType = FacilityType.PowerSupply; // 현재 선택 설비
    private Vector2 facilityScroll; // 설비 목록 스크롤 위치
    private Vector2 detailScroll; // 상세 정보 스크롤 위치
    private bool panelVisible; // 강화 패널 표시 여부

    private void Update() // 키보드 입력 처리
    {
        if (!IsLobbyScene()) // 로비 Scene 여부 확인
        {
            panelVisible = false; // 다른 Scene에서 강화 패널 숨김
            return; // 입력 처리 중단
        }

        Keyboard keyboard = Keyboard.current; // 현재 키보드 장치 조회
        if (keyboard != null && keyboard.f7Key.wasPressedThisFrame) // F7 입력 확인
        {
            panelVisible = !panelVisible; // 강화 패널 표시 상태 전환
        }
    }

    private void OnGUI() // 임시 설비 강화 UI 출력
    {
        if (!IsLobbyScene()) // 로비 Scene 여부 확인
        {
            return; // 다른 Scene에서 UI 출력 중단
        }

        GUI.Box(new Rect(12f, 12f, 220f, 32f), "F7 : Facility Upgrade"); // 강화 UI 단축키 안내
        if (!panelVisible) // 패널 숨김 여부 확인
        {
            return; // 상세 UI 출력 중단
        }

        FacilityUpgradeManager facilityManager = FacilityUpgradeManager.EnsureInstance(); // 설비 관리자 준비
        PlayerResourceManager resourceManager = PlayerResourceManager.EnsureInstance(); // 자원 관리자 준비
        GUILayout.BeginArea(new Rect(35f, 55f, 980f, 650f), GUI.skin.window); // 강화 화면 전체 영역 시작
        GUILayout.Label("35 Day - Ceruleon Facility Upgrade"); // 강화 화면 제목 출력
        GUILayout.Label($"Gold {resourceManager.Gold}   Screw {resourceManager.Screw}   Iron {resourceManager.IronPlate}   Wire {resourceManager.Wire}"); // 현재 보유 자원 출력
        GUILayout.Space(8f); // 상단 여백 추가
        GUILayout.BeginHorizontal(); // 설비 목록과 상세 영역 가로 배치 시작
        DrawFacilityList(facilityManager); // 좌측 설비 목록 출력
        DrawFacilityDetail(facilityManager); // 우측 선택 설비 상세 출력
        GUILayout.EndHorizontal(); // 가로 배치 종료
        GUILayout.Space(8f); // 하단 여백 추가
        if (GUILayout.Button("DEBUG : Gold / Screw / Iron / Wire +500", GUILayout.Height(34f))) // 테스트 자원 지급 버튼 출력
        {
            resourceManager.AddResources(500, 500, 500, 500); // 테스트용 자원 지급
        }
        GUILayout.EndArea(); // 강화 화면 전체 영역 종료
    }

    private void DrawFacilityList(FacilityUpgradeManager facilityManager) // 좌측 설비 목록 출력
    {
        GUILayout.BeginVertical(GUILayout.Width(350f)); // 설비 목록 세로 영역 시작
        GUILayout.Label("Facilities"); // 설비 목록 제목 출력
        facilityScroll = GUILayout.BeginScrollView(facilityScroll, GUILayout.Height(520f)); // 설비 목록 스크롤 시작
        Array facilityTypes = Enum.GetValues(typeof(FacilityType)); // 전체 설비 종류 조회
        foreach (FacilityType type in facilityTypes) // 전체 설비 순회
        {
            FacilityDefinition definition = facilityManager.GetDefinition(type); // 설비 데이터 조회
            int level = facilityManager.GetLevel(type); // 현재 설비 레벨 조회
            string buttonText = $"{definition.DisplayName} / {definition.EnglishName}   Lv.{level}"; // 설비 버튼 문구 생성
            if (GUILayout.Button(buttonText, GUILayout.Height(42f))) // 설비 선택 버튼 출력
            {
                selectedType = type; // 선택 설비 변경
            }
        }
        GUILayout.EndScrollView(); // 설비 목록 스크롤 종료
        GUILayout.EndVertical(); // 설비 목록 세로 영역 종료
    }

    private void DrawFacilityDetail(FacilityUpgradeManager facilityManager) // 선택 설비 상세 출력
    {
        FacilityDefinition definition = facilityManager.GetDefinition(selectedType); // 선택 설비 데이터 조회
        int currentLevel = facilityManager.GetLevel(selectedType); // 선택 설비 현재 레벨 조회
        FacilityLevelDefinition currentData = facilityManager.GetCurrentLevelDefinition(selectedType); // 현재 레벨 효과 조회
        FacilityLevelDefinition nextData = facilityManager.GetNextLevelDefinition(selectedType); // 다음 강화 데이터 조회
        GUILayout.BeginVertical(GUILayout.ExpandWidth(true)); // 상세 정보 세로 영역 시작
        detailScroll = GUILayout.BeginScrollView(detailScroll, GUILayout.Height(520f)); // 상세 정보 스크롤 시작
        GUILayout.Label($"{definition.DisplayName} / {definition.EnglishName}"); // 선택 설비 이름 출력
        GUILayout.Label($"Current Level : {currentLevel} / {FacilityUpgradeManager.MaximumLevel}"); // 현재 레벨 출력
        GUILayout.Space(12f); // 상세 정보 여백 추가
        GUILayout.Label("Current Effect"); // 현재 효과 제목 출력
        GUILayout.TextArea(currentData == null ? "No Effect" : currentData.EffectDescription, GUILayout.Height(70f)); // 현재 효과 설명 출력
        GUILayout.Space(12f); // 상세 정보 여백 추가
        GUILayout.Label("Next Effect"); // 다음 효과 제목 출력
        GUILayout.TextArea(nextData == null ? "MAX" : nextData.EffectDescription, GUILayout.Height(70f)); // 다음 효과 설명 출력
        GUILayout.Space(12f); // 상세 정보 여백 추가
        if (nextData != null) // 다음 강화 존재 확인
        {
            GUILayout.Label($"Cost : Gold {nextData.GoldCost} / Screw {nextData.ScrewCost} / Iron {nextData.IronPlateCost} / Wire {nextData.WireCost}"); // 다음 강화 비용 출력
            bool previousEnabled = GUI.enabled; // 기존 GUI 활성 상태 저장
            GUI.enabled = facilityManager.CanUpgrade(selectedType); // 자원 보유 상태에 따라 강화 버튼 활성화
            if (GUILayout.Button("UPGRADE", GUILayout.Height(48f))) // 강화 실행 버튼 출력
            {
                facilityManager.TryUpgrade(selectedType); // 선택 설비 강화 시도
            }
            GUI.enabled = previousEnabled; // 기존 GUI 활성 상태 복원
        }
        else // 최대 레벨 처리
        {
            GUILayout.Label("MAX LEVEL"); // 최대 강화 문구 출력
        }
        GUILayout.Space(16f); // 상세 정보 여백 추가
        GUILayout.Label("35 Day Runtime Values"); // 현재 런타임 값 제목 출력
        GUILayout.TextArea(GetRuntimeValueText(facilityManager, selectedType), GUILayout.Height(110f)); // 현재 런타임 보정 값 출력
        GUILayout.EndScrollView(); // 상세 정보 스크롤 종료
        GUILayout.EndVertical(); // 상세 정보 세로 영역 종료
    }

    private static string GetRuntimeValueText(FacilityUpgradeManager facilityManager, FacilityType type) // 설비별 현재 런타임 값 문구 생성
    {
        switch (type) // 설비 종류별 값 분기
        {
            case FacilityType.PowerSupply: // 전력 공급기 처리
                return $"Start Mental +{facilityManager.GetPowerSupplyMentalBonus()}\nFirst Turn AP +{facilityManager.GetPowerSupplyFirstTurnActionPointBonus()}"; // 전력 공급기 값 반환
            case FacilityType.DefenseBarrier: // 방어 차폐 장치 처리
                return $"Incoming Damage -{facilityManager.GetDefenseBarrierDamageReductionPercent()}%"; // 방어 차폐 값 반환
            case FacilityType.MagicConverter: // 마력 변환기 처리
                return $"Magic Damage +{facilityManager.GetMagicConverterDamageBonusPercent()}%"; // 마력 변환 값 반환
            case FacilityType.AutoRecovery: // 자율 회복 장치 처리
                return $"Battle End Recovery {facilityManager.GetAutoRecoveryPercent()}%"; // 자율 회복 값 반환
            case FacilityType.DataAnalyzer: // 데이터 분석기 처리
                return $"Weakness Damage +{facilityManager.GetDataAnalyzerWeaknessBonusPercent()}%"; // 데이터 분석 값 반환
            case FacilityType.WarehouseExpansion: // 물자 창고 확장 처리
                return $"Normal Resource Reward +{facilityManager.GetWarehouseResourceBonusPercent()}%"; // 물자 창고 값 반환
            case FacilityType.CombatTraining: // 전투 훈련실 처리
                return $"Attack +{facilityManager.GetCombatTrainingAttackBonusPercent()}%"; // 전투 훈련 값 반환
            case FacilityType.EmergencyRepair: // 응급 수복장치 처리
                return $"First Death Revive Chance {facilityManager.GetEmergencyRepairReviveChancePercent()}%"; // 응급 수복 값 반환
            case FacilityType.CommunicationStation: // 통신 기지국 처리
                return $"Event Discovery +{facilityManager.GetCommunicationEventDiscoveryBonusPercent()}%"; // 통신 기지국 값 반환
            case FacilityType.EnvironmentPurifier: // 환경 정화 장치 처리
                return $"Environment Damage -{facilityManager.GetEnvironmentDamageReductionPercent()}%"; // 환경 정화 값 반환
            default: // 미정 설비 처리
                return "No Runtime Value"; // 기본 문구 반환
        }
    }

    private static bool IsLobbyScene() // 현재 로비 Scene 여부 확인
    {
        return SceneManager.GetActiveScene().name == LobbySceneName; // 로비 Scene 일치 여부 반환
    }
}
