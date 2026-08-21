using System; // 기본 이벤트 기능 사용
using System.Collections.Generic; // 목록과 대기열 기능 사용
using UnityEngine; // 유니티 오류 로그 사용
public sealed class BattleEventDispatcher : IDisposable // 전투 공용 이벤트 발행기
{ // 클래스 시작
    private readonly List<BattleEventSubscription> subscriptions = new List<BattleEventSubscription>(); // 전체 구독 목록
    private readonly Queue<BattleEventContext> pendingEvents = new Queue<BattleEventContext>(); // 중첩 발행 대기열
    private long nextRegistrationOrder = 1; // 다음 구독 등록 순번
    private long nextEventSequence = 1; // 다음 이벤트 발행 순번
    private bool publishing; // 현재 이벤트 발행 여부
    private bool disposed; // 발행기 종료 여부
    public bool IsDisposed => disposed; // 발행기 종료 상태
    public IDisposable Subscribe(BattleEventType eventType, Action<BattleEventContext> handler, int priority = 0) // 이벤트 구독
    { // 이벤트 구독 시작
        if (disposed) // 발행기 종료 확인
        { // 구독 불가 처리 시작
            throw new ObjectDisposedException(nameof(BattleEventDispatcher)); // 종료 발행기 예외
        } // 구독 불가 처리 종료
        BattleEventSubscription subscription = new BattleEventSubscription(this, eventType, handler, priority, nextRegistrationOrder++); // 새 구독 정보 생성
        subscriptions.Add(subscription); // 전체 구독 목록 추가
        subscriptions.Sort(CompareSubscriptions); // 우선순위와 등록 순서 정렬
        return subscription; // 구독 해제 토큰 반환
    } // 이벤트 구독 종료
    public bool Publish(BattleEventContext eventContext) // 공용 이벤트 발행
    { // 이벤트 발행 시작
        if (disposed || eventContext == null || eventContext.Sequence > 0) // 발행 가능 상태 확인
        { // 발행 불가 처리 시작
            return false; // 이벤트 발행 실패 반환
        } // 발행 불가 처리 종료
        pendingEvents.Enqueue(eventContext); // 중첩 안전 대기열 추가
        if (publishing) // 기존 발행 진행 확인
        { // 중첩 발행 처리 시작
            return true; // 대기열 등록 성공 반환
        } // 중첩 발행 처리 종료
        publishing = true; // 발행 진행 상태 저장
        try // 발행 보호 시작
        { // 발행 보호 구역 시작
            while (pendingEvents.Count > 0 && !disposed) // 대기 이벤트 순회
            { // 대기 이벤트 처리 시작
                BattleEventContext nextEvent = pendingEvents.Dequeue(); // 다음 이벤트 꺼내기
                nextEvent.AssignSequence(nextEventSequence++); // 전투 내 발행 순번 지정
                Dispatch(nextEvent); // 구독자에게 이벤트 전달
            } // 대기 이벤트 처리 종료
        } // 발행 보호 구역 종료
        finally // 발행 상태 복구 시작
        { // 발행 상태 복구 구역 시작
            publishing = false; // 발행 진행 상태 해제
        } // 발행 상태 복구 구역 종료
        return true; // 이벤트 발행 성공 반환
    } // 이벤트 발행 종료
    internal void Unsubscribe(BattleEventSubscription subscription) // 지정 구독 제거
    { // 구독 제거 시작
        if (subscription != null) // 구독 존재 확인
        { // 유효 구독 처리 시작
            subscriptions.Remove(subscription); // 전체 구독 목록 제거
        } // 유효 구독 처리 종료
    } // 구독 제거 종료
    private void Dispatch(BattleEventContext eventContext) // 단일 이벤트 전달
    { // 이벤트 전달 시작
        List<BattleEventSubscription> snapshot = new List<BattleEventSubscription>(subscriptions); // 구독 변경 대비 목록 복사
        foreach (BattleEventSubscription subscription in snapshot) // 구독 목록 순회
        { // 개별 구독 전달 시작
            if (!subscription.IsActive || subscription.EventType != eventContext.EventType) // 활성 상태와 종류 확인
            { // 전달 제외 처리 시작
                continue; // 다음 구독 이동
            } // 전달 제외 처리 종료
            try // 구독 처리 보호 시작
            { // 구독 처리 보호 구역 시작
                subscription.Handler.Invoke(eventContext); // 구독 처리 함수 실행
            } // 구독 처리 보호 구역 종료
            catch (Exception exception) // 구독 처리 오류 확인
            { // 구독 오류 처리 시작
                Debug.LogException(exception); // 구독 오류 출력
            } // 구독 오류 처리 종료
        } // 개별 구독 전달 종료
    } // 이벤트 전달 종료
    private static int CompareSubscriptions(BattleEventSubscription left, BattleEventSubscription right) // 구독 실행 순서 비교
    { // 구독 비교 시작
        int priorityComparison = right.Priority.CompareTo(left.Priority); // 높은 우선순위 우선 비교
        return priorityComparison != 0 ? priorityComparison : left.RegistrationOrder.CompareTo(right.RegistrationOrder); // 같은 우선순위 등록 순서 비교
    } // 구독 비교 종료
    public void Dispose() // 공용 이벤트 발행기 종료
    { // 발행기 종료 시작
        if (disposed) // 기존 종료 확인
        { // 중복 종료 처리 시작
            return; // 발행기 종료 중단
        } // 중복 종료 처리 종료
        disposed = true; // 발행기 종료 상태 저장
        pendingEvents.Clear(); // 대기 이벤트 전체 제거
        foreach (BattleEventSubscription subscription in subscriptions) // 전체 구독 순회
        { // 구독 비활성화 시작
            subscription.Deactivate(); // 구독 발행기 참조 제거
        } // 구독 비활성화 종료
        subscriptions.Clear(); // 전체 구독 목록 제거
    } // 발행기 종료 종료
} // 클래스 종료
