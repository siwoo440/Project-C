using System; // 기본 인터페이스 기능 사용
internal sealed class BattleEventSubscription : IDisposable // 전투 이벤트 구독 정보
{ // 클래스 시작
    private BattleEventDispatcher dispatcher; // 구독 소유 발행기
    public BattleEventType EventType { get; } // 구독 이벤트 종류
    public Action<BattleEventContext> Handler { get; } // 구독 처리 함수
    public int Priority { get; } // 구독 우선순위
    public long RegistrationOrder { get; } // 구독 등록 순번
    public bool IsActive => dispatcher != null; // 구독 활성 여부
    public BattleEventSubscription(BattleEventDispatcher owner, BattleEventType eventType, Action<BattleEventContext> handler, int priority, long registrationOrder) // 구독 정보 생성자
    { // 생성자 시작
        dispatcher = owner ?? throw new ArgumentNullException(nameof(owner)); // 구독 소유 발행기 저장
        EventType = eventType; // 구독 이벤트 종류 저장
        Handler = handler ?? throw new ArgumentNullException(nameof(handler)); // 구독 처리 함수 저장
        Priority = priority; // 구독 우선순위 저장
        RegistrationOrder = registrationOrder; // 구독 등록 순번 저장
    } // 생성자 종료
    public void Dispose() // 구독 해제
    { // 구독 해제 시작
        BattleEventDispatcher currentDispatcher = dispatcher; // 현재 소유 발행기 저장
        dispatcher = null; // 구독 비활성화
        currentDispatcher?.Unsubscribe(this); // 발행기 구독 목록 제거
    } // 구독 해제 종료
    internal void Deactivate() // 발행기 종료 구독 비활성화
    { // 비활성화 시작
        dispatcher = null; // 발행기 참조 제거
    } // 비활성화 종료
} // 클래스 종료
