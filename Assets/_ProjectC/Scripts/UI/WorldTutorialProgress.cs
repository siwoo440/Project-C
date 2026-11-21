using System; // 안내 상태 변경 알림 사용
using UnityEngine; // 실제 탐사 위치 비교 사용
public enum WorldTutorialKind // 거점과 탐사 안내 종류
{ // 안내 종류 시작
    Lobby, // 거점 준비 안내
    Exploration // 탐사 이동 안내
} // 안내 종류 종료
public sealed class WorldTutorialProgress // 거점과 탐사 안내 진행 상태
{ // 안내 진행 상태 시작
    private static readonly (string Title, string Text, string Example)[] LobbyPages = // 실제 거점 안내의 제목과 설명 및 예시
    { // 거점 안내 목록 시작
        ("거점과 캐릭터", "이곳에서 캐릭터 상태를 확인하고 다음 탐사를 준비합니다. 캐릭터 버튼에서 보유 캐릭터를 볼 수 있습니다.", "Lobby"), // 기존 거점 1번째 안내
        ("탐사 파티 편성", "탐사를 누르고 구역을 선택한 뒤 파티를 편성합니다. 캐릭터를 빼도 보유 전용 카드와 강화는 사라지지 않습니다.", "Formation"), // 기존 거점 2번째 안내
        ("치료와 부활 대기", "치료 대기는 인정된 탐사 1회, 부활 대기는 2회입니다. 전투 턴 수와는 다릅니다.", "Lobby"), // 기존 거점 3번째 안내
        ("설비 강화", "탐사에서 얻은 자원으로 설비를 강화합니다. 설비별 현재 효과와 다음 효과, 필요 자원을 확인하세요.", "Facility"), // 기존 거점 4번째 안내
        ("다음 탐사 준비", "캐릭터 상태와 설비를 확인한 뒤 탐사를 눌러 구역을 선택하세요.", "Lobby") // 기존 거점 5번째 안내
    }; // 거점 안내 목록 종료
    private static readonly (string Title, string Text, string Example)[] ExplorationPages = // 탐사 순서별 제목과 안내 및 화면 이름
    { // 탐사 안내 목록 시작
        ("탐사 이동", "WASD 또는 방향키로 바닥을 따라 이동하세요. 한 걸음 움직이면 다음 안내가 열립니다.", "Exploration"), // 실제 이동과 지도 안내
        ("이벤트", "이벤트 표식에 접촉하면 선택지가 열립니다. 선택 하나를 누른 뒤 결과를 확인하세요.", "Event"), // 접촉과 이벤트 선택 안내
        ("보물", "보물 방의 표식에 닿으면 보상을 얻습니다. 공용 카드가 나왔다면 카드를 받을 파티원을 선택하세요. 나중에 선택을 누르면 이 방에서 다시 받을 수 있습니다.", "Treasure"), // 보상과 소유자 선택 안내
        ("휴식", "휴식 방에서는 파티의 체력과 정신력을 회복하고 카드를 강화할 수 있습니다. 화면에 표시된 휴식 위험도를 확인하세요.", "Rest"), // 휴식과 강화 안내
        ("상점", "상점 방에서 상품과 가격을 확인하세요. 공용 카드를 살 때는 받을 파티원을 고릅니다. 유물 판매 목록에서는 보유 유물의 판매 가격을 볼 수 있습니다.", "Shop") // 상점 거래 안내
    }; // 탐사 안내 목록 종료
    private readonly ISettingsStore store; // 기기별 안내 완료 저장소
    private readonly string completionKey; // 안내별 완료 기록 이름
    private Vector2 startingPosition; // 탐사 안내 시작 위치
    private bool movementObserved; // 실제 이동 확인 여부
    public WorldTutorialKind Kind { get; } // 현재 안내 종류 조회
    public int StepIndex { get; private set; } // 현재 안내 단계 번호
    private (string Title, string Text, string Example)[] Pages => Kind == WorldTutorialKind.Lobby ? LobbyPages : ExplorationPages; // 현재 안내에서 보여 줄 실제 목록
    internal (string Title, string Text, string Example) CurrentPage => Pages[StepIndex]; // 표시할 실제 안내 한 장
    public int StepCount => Pages.Length; // 실제 내용과 같은 안내 장수
    public bool IsActive { get; private set; } // 안내 표시 여부
    public bool CanAdvance => IsActive && (Kind != WorldTutorialKind.Exploration || StepIndex != 0 || movementObserved); // 실제 이동을 반영한 다음 단계 허용 여부
    public bool CanGoBack => IsActive && StepIndex > 0; // 이전 안내 페이지 이동 가능 여부
    public event Action Changed; // 안내 화면 갱신 알림
    public WorldTutorialProgress(WorldTutorialKind kind, ISettingsStore settingsStore) // 안내별 진행 상태 생성
    { // 안내 상태 생성 시작
        Kind = kind; // 현재 안내 종류 저장
        store = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore)); // 유효한 설정 저장소 보관
        completionKey = kind == WorldTutorialKind.Lobby ? "lobbyTutorialCompleted" : "explorationTutorialCompleted"; // 안내별 완료 기록 구분
    } // 안내 상태 생성 종료
    public bool StartIfNeeded(bool enabled, Vector2 position) // 설정과 완료 기록에 따른 자동 시작
    { // 자동 시작 확인 시작
        if (!enabled || IsActive || store.Read(completionKey, 0) == 1) // 자동 안내 설정과 완료 여부 확인
        { // 자동 시작 불가 처리 시작
            return false; // 자동 안내 시작 거부
        } // 자동 시작 불가 처리 종료
        Replay(position); // 처음부터 안내 시작
        return true; // 자동 안내 시작 성공 반환
    } // 자동 시작 확인 종료
    public void Replay(Vector2 position) // 안내 다시 보기 시작
    { // 다시 보기 시작
        startingPosition = position; // 현재 탐사 위치 기준 저장
        movementObserved = false; // 이전 이동 확인 기록 초기화
        StepIndex = 0; // 첫 안내 단계 선택
        IsActive = true; // 안내 표시 상태 설정
        Changed?.Invoke(); // 안내 화면 갱신 알림
    } // 다시 보기 종료
    public void ObservePosition(Vector2 position) // 실제 탐사 위치 변경 확인
    { // 탐사 이동 확인 시작
        if (!IsActive || Kind != WorldTutorialKind.Exploration || movementObserved) // 탐사 이동 확인 필요 여부 검사
        { // 이동 확인 불필요 처리 시작
            return; // 위치 검사 중단
        } // 이동 확인 불필요 처리 종료
        if ((position - startingPosition).sqrMagnitude >= 0.01f) // 이동 오차를 제외한 실제 위치 변경 확인
        { // 실제 이동 기록 시작
            movementObserved = true; // 실제 탐사 이동 기록
            Changed?.Invoke(); // 다음 버튼 활성 상태 갱신
        } // 실제 이동 기록 종료
    } // 탐사 이동 확인 종료
    public void RebasePosition(Vector2 position) // 맵 자동 배치 위치를 이동 기준으로 설정
    { // 이동 기준 갱신 시작
        if (IsActive && Kind == WorldTutorialKind.Exploration && !movementObserved) // 실제 이동 전 탐사 안내 확인
        { // 자동 배치 기준 갱신 시작
            startingPosition = position; // 맵 자동 이동을 제외한 새 기준 위치 저장
        } // 자동 배치 기준 갱신 종료
    } // 이동 기준 갱신 종료
    public bool Advance() // 다음 안내 또는 완료 진행
    { // 다음 안내 진행 시작
        if (!CanAdvance) // 실제 행동과 안내 진행 가능 여부 확인
        { // 진행 불가 처리 시작
            return false; // 단계 진행 거부
        } // 진행 불가 처리 종료
        StepIndex++; // 다음 안내 단계 이동
        if (StepIndex >= StepCount) // 마지막 안내 완료 확인
        { // 안내 완료 처리 시작
            Finish(); // 완료 기록과 화면 종료
        } // 안내 완료 처리 종료
        else // 남은 안내 확인
        { // 다음 안내 표시 시작
            Changed?.Invoke(); // 변경된 안내 화면 갱신
        } // 다음 안내 표시 종료
        return true; // 안내 진행 성공 반환
    } // 다음 안내 진행 종료
    public bool GoBack() // 이전 안내 페이지 이동
    { // 이전 안내 이동 시작
        if (!CanGoBack) // 이전 페이지 존재 확인
        { // 이전 페이지 없음 처리 시작
            return false; // 이전 안내 이동 거부
        } // 이전 페이지 없음 처리 종료
        StepIndex--; // 이전 안내 페이지 선택
        Changed?.Invoke(); // 변경된 안내 화면 갱신
        return true; // 이전 안내 이동 성공 반환
    } // 이전 안내 이동 종료
    public void Skip() // 현재 안내 건너뛰기
    { // 안내 건너뛰기 시작
        if (IsActive) // 진행 중인 안내 확인
        { // 안내 종료 시작
            Finish(); // 완료 기록과 화면 종료
        } // 안내 종료 종료
    } // 안내 건너뛰기 종료
    private void Finish() // 안내 종료와 완료 기록
    { // 안내 완료 시작
        store.Write(completionKey, 1); // 안내 종류별 기기 완료 기록 저장
        IsActive = false; // 안내 화면 종료 상태 설정
        Changed?.Invoke(); // 안내 종료 화면 갱신 알림
    } // 안내 완료 종료
} // 안내 진행 상태 종료
