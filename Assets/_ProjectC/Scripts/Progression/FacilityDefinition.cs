using System; // 열거형과 기본 기능 사용
using System.Collections.Generic; // 설비 데이터 목록과 사전 사용

public sealed class FacilityLevelDefinition // 설비 한 단계 강화 데이터
{
    public int Level { get; } // 강화 후 설비 레벨
    public int GoldCost { get; } // 필요한 골드
    public int ScrewCost { get; } // 필요한 나사
    public int IronPlateCost { get; } // 필요한 철판
    public int WireCost { get; } // 필요한 전선
    public string EffectDescription { get; } // 해당 레벨 효과 설명

    public FacilityLevelDefinition(int level, int goldCost, int screwCost, int ironPlateCost, int wireCost, string effectDescription) // 강화 단계 데이터 생성
    {
        Level = level; // 강화 레벨 저장
        GoldCost = goldCost; // 골드 비용 저장
        ScrewCost = screwCost; // 나사 비용 저장
        IronPlateCost = ironPlateCost; // 철판 비용 저장
        WireCost = wireCost; // 전선 비용 저장
        EffectDescription = effectDescription; // 효과 설명 저장
    }
}

public sealed class FacilityDefinition // 설비 전체 데이터
{
    private readonly List<FacilityLevelDefinition> levels; // 레벨별 강화 데이터 목록

    public FacilityType Type { get; } // 설비 종류
    public string DisplayName { get; } // 한글 설비 이름
    public string EnglishName { get; } // 영문 설비 이름
    public IReadOnlyList<FacilityLevelDefinition> Levels => levels; // 레벨 데이터 읽기 전용 조회

    public FacilityDefinition(FacilityType type, string displayName, string englishName, List<FacilityLevelDefinition> levelDefinitions) // 설비 데이터 생성
    {
        Type = type; // 설비 종류 저장
        DisplayName = displayName; // 한글 이름 저장
        EnglishName = englishName; // 영문 이름 저장
        levels = levelDefinitions ?? throw new ArgumentNullException(nameof(levelDefinitions)); // 레벨 데이터 저장
    }

    public FacilityLevelDefinition GetLevelDefinition(int level) // 지정 레벨 데이터 조회
    {
        if (level < 1 || level > levels.Count) // 유효 레벨 범위 확인
        {
            return null; // 범위 밖 데이터 없음 반환
        }

        return levels[level - 1]; // 지정 레벨 데이터 반환
    }
}

public static class FacilityCatalog // 세룰리온 설비 데이터 카탈로그
{
    private static readonly Dictionary<FacilityType, FacilityDefinition> Definitions = CreateDefinitions(); // 전체 설비 데이터 생성

    public static FacilityDefinition Get(FacilityType type) // 설비 데이터 조회
    {
        return Definitions[type]; // 종류에 맞는 설비 데이터 반환
    }

    public static IEnumerable<FacilityDefinition> GetAll() // 모든 설비 데이터 조회
    {
        return Definitions.Values; // 전체 설비 데이터 반환
    }

    private static Dictionary<FacilityType, FacilityDefinition> CreateDefinitions() // 전체 설비 데이터 생성
    {
        Dictionary<FacilityType, FacilityDefinition> definitions = new Dictionary<FacilityType, FacilityDefinition>(); // 설비 데이터 사전 생성
        definitions.Add(FacilityType.PowerSupply, CreatePowerSupply()); // 전력 공급기 등록
        definitions.Add(FacilityType.DefenseBarrier, CreateDefenseBarrier()); // 방어 차폐 장치 등록
        definitions.Add(FacilityType.MagicConverter, CreateMagicConverter()); // 마력 변환기 등록
        definitions.Add(FacilityType.AutoRecovery, CreateAutoRecovery()); // 자율 회복 장치 등록
        definitions.Add(FacilityType.DataAnalyzer, CreateDataAnalyzer()); // 데이터 분석기 등록
        definitions.Add(FacilityType.WarehouseExpansion, CreateWarehouseExpansion()); // 물자 창고 확장 등록
        definitions.Add(FacilityType.CombatTraining, CreateCombatTraining()); // 전투 훈련실 등록
        definitions.Add(FacilityType.EmergencyRepair, CreateEmergencyRepair()); // 응급 수복장치 등록
        definitions.Add(FacilityType.CommunicationStation, CreateCommunicationStation()); // 통신 기지국 등록
        definitions.Add(FacilityType.EnvironmentPurifier, CreateEnvironmentPurifier()); // 환경 정화 장치 등록
        return definitions; // 완성 설비 데이터 반환
    }

    private static FacilityDefinition CreatePowerSupply() // 전력 공급기 데이터 생성
    {
        List<FacilityLevelDefinition> levels = new List<FacilityLevelDefinition>(); // 강화 단계 목록 생성
        levels.Add(new FacilityLevelDefinition(1, 0, 5, 0, 10, "전투 시작 정신력 +3")); // 1레벨 데이터
        levels.Add(new FacilityLevelDefinition(2, 0, 10, 0, 15, "전투 시작 정신력 +6")); // 2레벨 데이터
        levels.Add(new FacilityLevelDefinition(3, 0, 15, 0, 20, "전투 시작 정신력 +9")); // 3레벨 데이터
        levels.Add(new FacilityLevelDefinition(4, 0, 20, 0, 25, "전투 시작 정신력 +12")); // 4레벨 데이터
        levels.Add(new FacilityLevelDefinition(5, 300, 25, 0, 30, "전투 시작 정신력 +15 / 첫 턴 AP +1")); // 5레벨 데이터
        return new FacilityDefinition(FacilityType.PowerSupply, "전력 공급기", "Power Supply", levels); // 전력 공급기 반환
    }

    private static FacilityDefinition CreateDefenseBarrier() // 방어 차폐 장치 데이터 생성
    {
        List<FacilityLevelDefinition> levels = new List<FacilityLevelDefinition>(); // 강화 단계 목록 생성
        levels.Add(new FacilityLevelDefinition(1, 0, 5, 10, 0, "아군 받는 피해 3% 감소")); // 1레벨 데이터
        levels.Add(new FacilityLevelDefinition(2, 0, 10, 15, 0, "아군 받는 피해 6% 감소")); // 2레벨 데이터
        levels.Add(new FacilityLevelDefinition(3, 0, 15, 20, 0, "아군 받는 피해 9% 감소")); // 3레벨 데이터
        levels.Add(new FacilityLevelDefinition(4, 0, 20, 25, 0, "아군 받는 피해 12% 감소")); // 4레벨 데이터
        levels.Add(new FacilityLevelDefinition(5, 300, 25, 30, 0, "아군 받는 피해 15% 감소 / 탐사 첫 전투 첫 피해 50% 감소")); // 5레벨 데이터
        return new FacilityDefinition(FacilityType.DefenseBarrier, "방어 차폐 장치", "Defense Barrier", levels); // 방어 차폐 장치 반환
    }

    private static FacilityDefinition CreateMagicConverter() // 마력 변환기 데이터 생성
    {
        List<FacilityLevelDefinition> levels = new List<FacilityLevelDefinition>(); // 강화 단계 목록 생성
        levels.Add(new FacilityLevelDefinition(1, 0, 8, 0, 8, "마법 / 스킬 카드 피해량 +5%")); // 1레벨 데이터
        levels.Add(new FacilityLevelDefinition(2, 0, 12, 0, 12, "마법 / 스킬 카드 피해량 +8%")); // 2레벨 데이터
        levels.Add(new FacilityLevelDefinition(3, 0, 16, 0, 16, "마법 / 스킬 카드 피해량 +11%")); // 3레벨 데이터
        levels.Add(new FacilityLevelDefinition(4, 0, 20, 0, 20, "마법 / 스킬 카드 피해량 +14%")); // 4레벨 데이터
        levels.Add(new FacilityLevelDefinition(5, 300, 25, 0, 25, "마법 / 스킬 카드 피해량 +17% / 마력 회복 주기 1턴 단축")); // 5레벨 데이터
        return new FacilityDefinition(FacilityType.MagicConverter, "마력 변환기", "Magic Converter", levels); // 마력 변환기 반환
    }

    private static FacilityDefinition CreateAutoRecovery() // 자율 회복 장치 데이터 생성
    {
        List<FacilityLevelDefinition> levels = new List<FacilityLevelDefinition>(); // 강화 단계 목록 생성
        levels.Add(new FacilityLevelDefinition(1, 0, 0, 8, 5, "전투 종료 시 가장 HP가 낮은 아군 1명 HP 3% 회복")); // 1레벨 데이터
        levels.Add(new FacilityLevelDefinition(2, 0, 0, 12, 10, "전투 종료 시 가장 HP가 낮은 아군 1명 HP 5% 회복")); // 2레벨 데이터
        levels.Add(new FacilityLevelDefinition(3, 0, 0, 16, 15, "전투 종료 시 모든 아군 HP 5% 회복")); // 3레벨 데이터
        levels.Add(new FacilityLevelDefinition(4, 0, 0, 20, 20, "전투 종료 시 모든 아군 HP 7% 회복")); // 4레벨 데이터
        levels.Add(new FacilityLevelDefinition(5, 300, 0, 25, 25, "전투 종료 시 모든 아군 HP 10% 회복 / 탐사당 3회")); // 5레벨 데이터
        return new FacilityDefinition(FacilityType.AutoRecovery, "자율 회복 장치", "Auto Recovery", levels); // 자율 회복 장치 반환
    }

    private static FacilityDefinition CreateDataAnalyzer() // 데이터 분석기 데이터 생성
    {
        List<FacilityLevelDefinition> levels = new List<FacilityLevelDefinition>(); // 강화 단계 목록 생성
        levels.Add(new FacilityLevelDefinition(1, 0, 6, 0, 6, "적 정보 표시: HP / 속성")); // 1레벨 데이터
        levels.Add(new FacilityLevelDefinition(2, 0, 10, 0, 10, "적 약점 표시")); // 2레벨 데이터
        levels.Add(new FacilityLevelDefinition(3, 0, 15, 0, 15, "적의 다음 행동 일부 표시")); // 3레벨 데이터
        levels.Add(new FacilityLevelDefinition(4, 0, 20, 0, 20, "약점 공격 시 피해 +10%")); // 4레벨 데이터
        levels.Add(new FacilityLevelDefinition(5, 300, 25, 0, 25, "약점 공격 시 피해 +15% / 보스 특수 패턴 예고")); // 5레벨 데이터
        return new FacilityDefinition(FacilityType.DataAnalyzer, "데이터 분석기", "Data Analyzer", levels); // 데이터 분석기 반환
    }

    private static FacilityDefinition CreateWarehouseExpansion() // 물자 창고 확장 데이터 생성
    {
        List<FacilityLevelDefinition> levels = new List<FacilityLevelDefinition>(); // 강화 단계 목록 생성
        levels.Add(new FacilityLevelDefinition(1, 0, 5, 10, 0, "일반 자원 획득량 +5%")); // 1레벨 데이터
        levels.Add(new FacilityLevelDefinition(2, 0, 10, 15, 0, "일반 자원 획득량 +10%")); // 2레벨 데이터
        levels.Add(new FacilityLevelDefinition(3, 0, 15, 20, 0, "일반 자원 획득량 +15%")); // 3레벨 데이터
        levels.Add(new FacilityLevelDefinition(4, 0, 20, 25, 0, "일반 자원 획득량 +20%")); // 4레벨 데이터
        levels.Add(new FacilityLevelDefinition(5, 300, 25, 30, 0, "일반 자원 획득량 +25% / 희귀 재료 드랍률 +5%")); // 5레벨 데이터
        return new FacilityDefinition(FacilityType.WarehouseExpansion, "물자 창고 확장", "Warehouse Expansion", levels); // 물자 창고 확장 반환
    }

    private static FacilityDefinition CreateCombatTraining() // 전투 훈련실 데이터 생성
    {
        List<FacilityLevelDefinition> levels = new List<FacilityLevelDefinition>(); // 강화 단계 목록 생성
        levels.Add(new FacilityLevelDefinition(1, 0, 5, 5, 0, "아군 공격력 +3%")); // 1레벨 데이터
        levels.Add(new FacilityLevelDefinition(2, 0, 10, 10, 0, "아군 공격력 +6%")); // 2레벨 데이터
        levels.Add(new FacilityLevelDefinition(3, 0, 15, 15, 0, "아군 공격력 +9% / 훈련 임무 해금")); // 3레벨 데이터
        levels.Add(new FacilityLevelDefinition(4, 0, 20, 20, 0, "아군 공격력 +12%")); // 4레벨 데이터
        levels.Add(new FacilityLevelDefinition(5, 300, 25, 25, 0, "아군 공격력 +15% / 첫 턴 공격 카드 피해 +10%")); // 5레벨 데이터
        return new FacilityDefinition(FacilityType.CombatTraining, "전투 훈련실", "Combat Training", levels); // 전투 훈련실 반환
    }

    private static FacilityDefinition CreateEmergencyRepair() // 응급 수복장치 데이터 생성
    {
        List<FacilityLevelDefinition> levels = new List<FacilityLevelDefinition>(); // 강화 단계 목록 생성
        levels.Add(new FacilityLevelDefinition(1, 0, 0, 12, 6, "탐사 첫 사망 시 20% 확률 HP 10% 부활")); // 1레벨 데이터
        levels.Add(new FacilityLevelDefinition(2, 0, 0, 16, 10, "탐사 첫 사망 시 35% 확률 HP 15% 부활")); // 2레벨 데이터
        levels.Add(new FacilityLevelDefinition(3, 0, 0, 20, 14, "탐사 첫 사망 시 50% 확률 HP 20% 부활")); // 3레벨 데이터
        levels.Add(new FacilityLevelDefinition(4, 0, 0, 24, 18, "탐사 첫 사망 시 75% 확률 HP 25% 부활")); // 4레벨 데이터
        levels.Add(new FacilityLevelDefinition(5, 400, 0, 28, 22, "탐사 첫 사망 시 1회 확정 부활 / HP 30% 회복")); // 5레벨 데이터
        return new FacilityDefinition(FacilityType.EmergencyRepair, "응급 수복장치", "Emergency Repair", levels); // 응급 수복장치 반환
    }

    private static FacilityDefinition CreateCommunicationStation() // 통신 기지국 데이터 생성
    {
        List<FacilityLevelDefinition> levels = new List<FacilityLevelDefinition>(); // 강화 단계 목록 생성
        levels.Add(new FacilityLevelDefinition(1, 0, 4, 0, 10, "탐사 이벤트 발견률 +5%")); // 1레벨 데이터
        levels.Add(new FacilityLevelDefinition(2, 0, 8, 0, 14, "탐사 이벤트 발견률 +10%")); // 2레벨 데이터
        levels.Add(new FacilityLevelDefinition(3, 0, 12, 0, 18, "탐사 이벤트 발견률 +15% / 위험 이벤트 사전 경고")); // 3레벨 데이터
        levels.Add(new FacilityLevelDefinition(4, 0, 16, 0, 22, "탐사 이벤트 발견률 +20%")); // 4레벨 데이터
        levels.Add(new FacilityLevelDefinition(5, 300, 20, 0, 26, "탐사 이벤트 발견률 +25% / 보상 이벤트 등장 확률 +5%")); // 5레벨 데이터
        return new FacilityDefinition(FacilityType.CommunicationStation, "통신 기지국", "Communication Station", levels); // 통신 기지국 반환
    }

    private static FacilityDefinition CreateEnvironmentPurifier() // 환경 정화 장치 데이터 생성
    {
        List<FacilityLevelDefinition> levels = new List<FacilityLevelDefinition>(); // 강화 단계 목록 생성
        levels.Add(new FacilityLevelDefinition(1, 0, 0, 10, 10, "퇴색 지역 환경 피해 10% 감소")); // 1레벨 데이터
        levels.Add(new FacilityLevelDefinition(2, 0, 0, 14, 14, "퇴색 지역 환경 피해 15% 감소")); // 2레벨 데이터
        levels.Add(new FacilityLevelDefinition(3, 0, 0, 18, 18, "퇴색 지역 환경 피해 20% 감소 / 정신력 감소량 5% 감소")); // 3레벨 데이터
        levels.Add(new FacilityLevelDefinition(4, 0, 0, 22, 22, "퇴색 지역 환경 피해 25% 감소")); // 4레벨 데이터
        levels.Add(new FacilityLevelDefinition(5, 300, 0, 26, 26, "퇴색 지역 환경 피해 30% 감소 / 퇴색 이벤트 위험도 1단계 감소")); // 5레벨 데이터
        return new FacilityDefinition(FacilityType.EnvironmentPurifier, "환경 정화 장치", "Environment Purifier", levels); // 환경 정화 장치 반환
    }
}
