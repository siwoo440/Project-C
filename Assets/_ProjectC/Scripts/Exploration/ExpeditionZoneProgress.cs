using System; // 저장 직렬화와 수치 한계 처리

[Serializable] // 거점과 탐사 저장에 같은 구역 상태 사용
internal sealed class ExpeditionZoneProgressSnapshot // 현재 슬롯에 보관할 독립 구역 진행도
{ // 구역 기록 저장 시작
    public int version; // 선택 필드의 지원 형식 번호
    public int lastZone; // 마지막 실제 출발 구역의 번호
    public int lastLevel; // 마지막 실제 출발 탐사 레벨
    public bool hasZoneIdentificationCode; // 실제 협곡 보스가 지급한 영구 식별 코드
    public bool mainStoryReported; // 실제 성채 승리 뒤 확정한 원정 기록 보고
    public ExpeditionZoneState[] zones; // 여덟 구역의 서로 독립적인 상태
} // 구역 기록 저장 종료

[Serializable] // 구역별 최고 레벨과 누적 상태 직렬화
internal sealed class ExpeditionZoneState // 실제 슬롯의 구역별 진행 규칙 저장
{ // 개별 구역 기록 시작
    public int percent; // 최근 공략의 실제 탐사 수치
    public int highestLevel = 1; // 이미 참여할 수 있는 구역 최고 레벨
    public int fullCount; // 탐사 수치 백 퍼센트에 도달한 누적 횟수
    public bool bossCleared; // 실제 관문 보스를 처치한 기록
    public bool defeatCredited; // 이 구역 전멸로 한 점을 받은 기록
    public bool retreatCredited; // 이 구역 후퇴 또는 도주로 한 점을 받은 기록

    public ExpeditionZoneState Copy() // 획득 기록을 원본과 독립된 상태로 복제
    { // 저장용 개별 구역 복사 시작
        return (ExpeditionZoneState)MemberwiseClone(); // 현재 숫자와 선택 여부의 독립 사본
    } // 저장용 개별 구역 복사 종료
} // 개별 구역 기록 종료

public static class ExpeditionZoneProgress // 현재 저장 슬롯의 고정 세계 지도 진행 규칙
{ // 실제 구역 진행 관리자 시작
    private const int FullProgress = 100; // 보스 공략을 예약할 탐사 수치
    private const int SaveVersion = 1; // 선택과 진행을 지원하는 첫 저장 형식
    private static ExpeditionZoneState[] zones = CreateStates(); // 현재 슬롯의 독립 구역 여덟 개
    public static ExpeditionZoneId LastSelectedZone { get; private set; } // 실제 마지막 출발 구역
    public static int LastSelectedLevel { get; private set; } = 1; // 실제 마지막 출발 난이도
    public static bool HasZoneIdentificationCode { get; private set; } // 영구 구역 식별 코드 보유 여부
    public static bool HasReportedMainStory { get; private set; } // 후속 지도 해금에 필요한 실제 원정 보고 기록
    public static bool CanReportMainStory => HasBossClear(ExpeditionZoneId.RuinedCitadel) && !HasReportedMainStory; // 성채를 공략하고 아직 보고하지 않은 상태

    public static bool IsUnlocked(ExpeditionZoneId id) // 출시 순서의 실제 출전 허용 조건 확인
    { // 구역별 선행 보스와 탐사 기록 검사 시작
        return id switch // 공식 세계 지도 해금 조건 선택
        { // 각 구역의 초반 잠금 조건 시작
            ExpeditionZoneId.WinterfallAshPlains => true, // 원래 윈터폴은 기본 참여 가능
            ExpeditionZoneId.FracturedEarth => HasBossClear(ExpeditionZoneId.WinterfallAshPlains), // 실제 윈터폴 보스 공략 기록
            ExpeditionZoneId.CorrodedIndustrialDistrict => GetState(ExpeditionZoneId.FracturedEarth).fullCount >= 1, // 금 간 대지의 첫 백 퍼센트 도달
            ExpeditionZoneId.FadedDunes => HasBossClear(ExpeditionZoneId.CorrodedIndustrialDistrict), // 산업 지구의 실제 보스 공략 기록
            ExpeditionZoneId.ScorchedRavine => HasBossClear(ExpeditionZoneId.FadedDunes), // 퇴색의 사막의 실제 보스 공략 기록
            ExpeditionZoneId.SubterraneanVault => HasBossClear(ExpeditionZoneId.ScorchedRavine) && HasZoneIdentificationCode, // 협곡 보스와 영구 식별 코드 동시 확인
            ExpeditionZoneId.RuinedCitadel => GetState(ExpeditionZoneId.SubterraneanVault).fullCount >= 2, // 지하 벙커에서 새 진행도 백 퍼센트 두 차례
            ExpeditionZoneId.TheVoid => HasBossClear(ExpeditionZoneId.RuinedCitadel) && HasReportedMainStory, // 성채 보스와 별개의 메인 스토리 보고 확인
            _ => false // 정의되지 않은 저장 번호의 출전 거절
        }; // 각 구역의 초반 잠금 조건 종료
    } // 구역별 선행 보스와 탐사 기록 검사 종료

    public static int GetPercent(ExpeditionZoneId id) => GetState(id)?.percent ?? 0; // 해당 구역만의 현재 탐사 수치
    public static int GetHighestLevel(ExpeditionZoneId id) => GetState(id)?.highestLevel ?? 0; // 해당 구역만의 최고 해금 레벨
    public static bool HasPendingBoss(ExpeditionZoneId id) => GetPercent(id) == FullProgress; // 다음 탐사에 보스가 예약된 구역 확인

    internal static bool CanSelect(ExpeditionZoneId id, int level) // 출발할 때 현재 구역과 해금 레벨 다시 확인
    { // 실제 출전 가능 여부 검사 시작
        return IsUnlocked(id) && level >= 1 && level <= GetHighestLevel(id); // 잠금과 최고 높이 사이 실제 출전 범위
    } // 실제 출전 가능 여부 검사 종료

    internal static bool TrySelect(ExpeditionZoneId id, int level) // 유효한 실제 출발 대상으로 마지막 선택 저장
    { // 선택한 구역과 레벨 저장 시작
        if (!CanSelect(id, level)) // 최종 출발 조건 비교
        { // 잘못된 요청 제외 시작
            return false; // 이전 실제 출발 선택 보존
        } // 잘못된 요청 제외 종료
        LastSelectedZone = id; // 실제 출발하는 구역으로 갱신
        LastSelectedLevel = level; // 출발 화면에서 확정된 단계로 갱신
        return true; // 이번 실제 출전 선택 기록 완료
    } // 선택한 구역과 레벨 저장 종료

    internal static bool CanReserveBoss(ExpeditionZoneId id, int level) // 새로 출발하는 탐사의 예약 보스 조건 확인
    { // 가장 최근 수치의 새 입장 시점 확인 시작
        return CanSelect(id, level) && HasPendingBoss(id); // 실제 선택 난이도와 출발 시점의 백 퍼센트
    } // 가장 최근 수치의 새 입장 시점 확인 종료

    internal static void RecordBattleVictory(ExpeditionZoneId id, BattleType type) // 실제 비보스 전투 승리에 따른 탐사 수치
    { // 실제 승리 등급의 점수 처리 시작
        int amount = type == BattleType.Normal ? 4 : type == BattleType.Elite ? 8 : 0; // 일반 사 점과 정예 팔 점 및 나머지 영 점
        AddProgress(GetState(id), amount); // 해당 지역의 보스 예약 기준에 반영
    } // 실제 승리 등급의 점수 처리 종료

    internal static void RecordEvent(ExpeditionZoneId id, bool successful) // 실제 사건 판정의 성공 및 실패 감점 차이
    { // 처리 완료한 사건 점수 지급 시작
        AddProgress(GetState(id), successful ? 6 : 3); // 성공 육 점과 실패 삼 점
    } // 처리 완료한 사건 점수 지급 종료

    internal static void RecordDefeat(ExpeditionZoneId id) // 이 구역 최초 전멸의 독립 탐사 보정
    { // 전멸 보정 사용 여부 확인 시작
        var state = GetState(id); // 실제 전멸이 일어난 구역의 기록
        if (state is { defeatCredited: false }) // 구역당 한 번의 전멸 보정 확인
        { // 처음 전멸 기록 적용 시작
            state.defeatCredited = true; // 이후 실제 전멸의 반복 반영 방지
            AddProgress(state, 1); // 문서의 처음 전멸 한 점
        } // 처음 전멸 기록 적용 종료
    } // 전멸 보정 사용 여부 확인 종료

    internal static void RecordRetreat(ExpeditionZoneId id) // 이 구역 처음 실제 후퇴 또는 도주의 탐사 보정
    { // 도주 보정 사용 여부 확인 시작
        var state = GetState(id); // 실제 도주가 확정된 구역 기록
        if (state is { retreatCredited: false }) // 구역당 한 번의 후퇴 보정 확인
        { // 처음 후퇴 기록 적용 시작
            state.retreatCredited = true; // 다음 원정과 난이도에 같은 기록 유지
            AddProgress(state, 1); // 문서의 처음 후퇴 한 점
        } // 처음 후퇴 기록 적용 종료
    } // 도주 보정 사용 여부 확인 종료

    internal static void RecordBossVictory(ExpeditionZoneId id, int selectedLevel, bool wasReserved) // 실제 원본 관문 보스 승리만 반영
    { // 구역 해금과 최고 공략 진행의 독립 정산 시작
        var state = GetState(id); // 관문 보스를 처치한 지역의 독립 상태
        if (state == null || !CanSelect(id, selectedLevel)) // 기존 실제 공략 대상과 선택 난이도 확인
        { // 유효하지 않은 원정 확인 시작
            return; // 진행이나 해금 조건 변경 없음
        } // 유효하지 않은 원정 확인 종료
        state.bossCleared = true; // 다음 구역의 실제 보스 처치 조건 기록
        if (id == ExpeditionZoneId.ScorchedRavine) // 구역 식별 코드를 얻는 실제 협곡 보스 확인
        { // 기존 보유와 무관한 영구 코드 지급 시작
            HasZoneIdentificationCode = true; // 실제 관문 처치 보상으로 영구 지역 코드 지급
        } // 기존 보유와 무관한 영구 코드 지급 종료
        if (wasReserved && selectedLevel == state.highestLevel && state.percent == FullProgress && state.highestLevel < int.MaxValue) // 출발 전 예약과 최고 선택의 현재 유효성 확인
        { // 최고 레벨 공략을 완료한 새 진행도 시작
            state.highestLevel++; // 현재 구역 한 단계 해금
            state.percent = 0; // 같은 구역의 다음 공략 수치 초기화
        } // 최고 레벨 공략을 완료한 새 진행도 종료
    } // 구역 해금과 최고 공략 진행의 독립 정산 종료

    public static bool RecordMainStoryCompletion() // 실제 성채 승리 후 명시적인 원정 기록 보고
    { // 성채 보고 조건과 중복 확인 시작
        if (!CanReportMainStory) // 실제 정산한 성채와 미보고 상태 확인
        { // 잘못된 보고 입력 차단 시작
            return false; // 기존 진행도 유지
        } // 잘못된 보고 입력 차단 종료
        HasReportedMainStory = true; // 메인 스토리 보고를 저장할 독립 표시
        return true; // 실제 지도 해금에 반영할 보고 완료
    } // 성채 보고 조건과 중복 확인 종료

    private static void AddProgress(ExpeditionZoneState state, int amount) // 실제 탐사 수치와 지속 백 퍼센트 이력 처리
    { // 새 탐사 수치 경계 계산 시작
        if (state == null || amount <= 0 || state.percent >= FullProgress) // 값 없음과 미사용 및 보스 예약 수치 확인
        { // 이전 탐사 수치를 유지하는 경우 시작
            return; // 이미 백 퍼센트에서 다음 출발을 기다리는 처리
        } // 이전 탐사 수치를 유지하는 경우 종료
        state.percent = Math.Min(FullProgress, state.percent + amount); // 초과분 없이 백 퍼센트에서 정지
        if (state.percent == FullProgress && state.fullCount < int.MaxValue) // 새 사이클에서 처음 도달한 백 퍼센트 확인
        { // 공식 지역 해금에 사용하는 이력 기록 시작
            state.fullCount++; // 같은 공략에서 백 퍼센트 이력 한 번 누적
        } // 공식 지역 해금에 사용하는 이력 기록 종료
    } // 새 탐사 수치 경계 계산 종료

    internal static ExpeditionZoneProgressSnapshot Capture() // 거점과 회차 저장에 현재 구역 상태 복사
    { // 현재 슬롯의 저장 가능한 값 구성 시작
        var copies = new ExpeditionZoneState[zones.Length]; // 현재 상태와 독립인 저장 목록 준비
        for (int index = 0; index < copies.Length; index++) // 안정된 숫자 순서의 전체 구역 복사
        { // 각 구역 상태 복사 시작
            copies[index] = zones[index].Copy(); // 진행도의 이후 변경을 방지한 개별 사본
        } // 각 구역 상태 복사 종료
        return new ExpeditionZoneProgressSnapshot // 거점 저장에 넣는 현재 캠페인 기록
        { // 저장 필드 구성 시작
            version = SaveVersion, // 현재 출전 진행의 기록 형식
            lastZone = (int)LastSelectedZone, // 실제 출발 구역의 고정 번호
            lastLevel = LastSelectedLevel, // 실제 출발한 마지막 난이도
            hasZoneIdentificationCode = HasZoneIdentificationCode, // 영구 협곡 보상 상태
            mainStoryReported = HasReportedMainStory, // 별개 성채 원정 보고 상태
            zones = copies // 슬롯의 독립 여덟 구역 기록
        }; // 저장 필드 구성 종료
    } // 현재 슬롯의 저장 가능한 값 구성 종료

    internal static bool IsValidSnapshot(ExpeditionZoneProgressSnapshot snapshot) // 이전 거점 슬롯과 새 구역 기록을 모두 검사
    { // 구역 기록의 옵션 저장 검증 시작
        if (IsUnrecordedSnapshot(snapshot)) // 선택 필드가 없었던 기존 거점 저장 확인
        { // 이전 정상 저장 호환 시작
            return true; // 초기 윈터폴 선택으로 복원 허용
        } // 이전 정상 저장 호환 종료
        if (snapshot.version != SaveVersion || snapshot.zones == null || snapshot.zones.Length != ExpeditionZones.All.Count || // 현재 형식과 새 구역 상태 개수 확인
            !IsValidRunSelection(SaveVersion, snapshot.lastZone, snapshot.lastLevel, false)) // 선택한 출발 번호와 난이도 범위 확인
        { // 잘못된 저장 형태 처리 시작
            return false; // 기존 다른 저장의 상태 보호
        } // 잘못된 저장 형태 처리 종료
        foreach (var state in snapshot.zones) // 모든 구역의 독립 저장값 검사
        { // 개별 수치 검사 시작
            if (state == null || state.highestLevel < 1 || state.percent < 0 || state.percent > FullProgress || state.fullCount < 0 || // 누락 및 음수와 현재 수치 한계 확인
                state.highestLevel - 1 + (state.percent == FullProgress ? 1L : 0L) > state.fullCount) // 최고 해금과 현재 예약을 뒷받침하는 누적 공략 횟수 확인
            { // 기록 불일치 검사 시작
                return false; // 구역 진행 저장 오류 및 이전 정상본 복원
            } // 기록 불일치 검사 종료
        } // 개별 수치 검사 종료
        return snapshot.lastLevel <= snapshot.zones[snapshot.lastZone].highestLevel && // 출발한 난이도를 실제 선택 해금 범위와 비교
            (!snapshot.hasZoneIdentificationCode || snapshot.zones[(int)ExpeditionZoneId.ScorchedRavine].bossCleared) && // 해당 영구 코드의 실제 관문 출처 확인
            (!snapshot.mainStoryReported || snapshot.zones[(int)ExpeditionZoneId.RuinedCitadel].bossCleared); // 메인 스토리 보고의 실제 성채 보스 출처 확인
    } // 구역 기록의 옵션 저장 검증 종료

    private static bool IsUnrecordedSnapshot(ExpeditionZoneProgressSnapshot snapshot) // 구역 기록 도입 전 저장의 빈 진행 상태 판별
    { // 구형 거점의 누락된 구역 기록 확인 시작
        return snapshot == null || // 구역 진행 참조가 없는 이전 저장
            snapshot.version == 0 && snapshot.lastZone == 0 && snapshot.lastLevel == 0 && // Unity가 채운 빈 구역 형식의 기본값
            !snapshot.hasZoneIdentificationCode && !snapshot.mainStoryReported && // 저장된 영구 구역 진척이 없는 상태
            snapshot.zones is not { Length: > 0 }; // 실제 구역별 기록이 없는 이전 저장
    } // 구형 거점의 누락된 구역 기록 확인 종료

    internal static bool IsValidRunSelection(int version, int zone, int level, bool reserved) // 진행 중 회차의 고정 출발 기록 형식 확인
    { // 기존 회차 또는 현재 탐사 선택 검사 시작
        return version == 0 ? zone == 0 && level == 0 && !reserved : // 이전 탐사 저장의 누락 필드 허용
            version == SaveVersion && level >= 1 && ExpeditionZones.Get((ExpeditionZoneId)zone) != null; // 지원하는 구역 번호와 양수 선택 레벨 확인
    } // 기존 회차 또는 현재 탐사 선택 검사 종료

    internal static void Restore(ExpeditionZoneProgressSnapshot snapshot) // 거점 슬롯의 독립 상태를 현재 상태로 복원
    { // 이전 또는 현재 저장 복원 시작
        Reset(); // 다른 슬롯과 이전 실행의 진행 이력 제거
        if (IsUnrecordedSnapshot(snapshot)) // Unity가 채운 빈 상태를 포함한 이전 구역 정보 확인
        { // 이전 저장의 윈터폴 기본값 유지 시작
            return; // 새 게임의 처음 구역과 난이도로 복원
        } // 이전 저장의 윈터폴 기본값 유지 종료
        for (int index = 0; index < zones.Length; index++) // 실제 슬롯의 공식 순서로 모든 상태 복원
        { // 슬롯과 현재 상태를 분리한 복원 시작
            zones[index] = snapshot.zones[index].Copy(); // 과거 저장과 독립인 현재 진행도
        } // 슬롯과 현재 상태를 분리한 복원 종료
        LastSelectedZone = (ExpeditionZoneId)snapshot.lastZone; // 실제로 마지막 출발한 구역 복원
        LastSelectedLevel = snapshot.lastLevel; // 실제로 마지막 출발한 단계 복원
        HasZoneIdentificationCode = snapshot.hasZoneIdentificationCode; // 영구 협곡 식별 코드 복원
        HasReportedMainStory = snapshot.mainStoryReported; // 성채에 대한 별도 원정 보고 복원
    } // 이전 또는 현재 저장 복원 종료

    internal static void Reset() // 기존 계정 기록을 섞지 않는 새 슬롯 초기화
    { // 구역별 초기 상태 준비 시작
        zones = CreateStates(); // 기존 모든 구역 진행의 새 사본
        LastSelectedZone = ExpeditionZoneId.WinterfallAshPlains; // 기본 출발 지역 윈터폴
        LastSelectedLevel = 1; // 기본 탐사 난이도의 첫 단계
        HasZoneIdentificationCode = false; // 새 슬롯의 아직 획득하지 않은 코드
        HasReportedMainStory = false; // 새 슬롯의 보고 전 기본 상태
    } // 구역별 초기 상태 준비 종료

    private static ExpeditionZoneState[] CreateStates() // 여덟 고정 구역 각각의 단계와 탐사 수치 초기화
    { // 새 구역 기록 배열 구성 시작
        var result = new ExpeditionZoneState[ExpeditionZones.All.Count]; // 구역 수만큼 분리된 기록 공간 준비
        for (int index = 0; index < result.Length; index++) // 새 진행 기록에 필요한 개별 슬롯 순회
        { // 구역별 기본 진행 생성 시작
            result[index] = new ExpeditionZoneState(); // 기본 첫 레벨과 빈 진행 기록 설정
        } // 구역별 기본 진행 생성 종료
        return result; // 기존 구역과 섞이지 않는 초기 진행도
    } // 새 구역 기록 배열 구성 종료

    private static ExpeditionZoneState GetState(ExpeditionZoneId id) => ExpeditionZones.Get(id) == null ? null : zones[(int)id]; // 유효한 구역 번호에 대한 실제 상태 조회
    private static bool HasBossClear(ExpeditionZoneId id) => GetState(id)?.bossCleared == true; // 유효한 실제 보스 승리 기록 조회
} // 실제 구역 진행 관리자 종료
