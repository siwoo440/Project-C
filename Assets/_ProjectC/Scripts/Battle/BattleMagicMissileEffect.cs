using System.Collections; // 게임 화면 프레임별 연출
using UnityEngine; // 전투 좌표와 시간 계산
using UnityEngine.UI; // 캔버스의 투명 스프라이트 표시

internal sealed class BattleMagicMissileEffect // 마력탄 전용 시전 및 명중 표시
{ // 마력탄 전투 연출 시작
    private const string ResourceRoot = "UI/Battle/Effects/"; // 게임 번들 안의 이펙트 원화 폴더
    private const float CastDuration = 0.21f; // 시전자 앞에 빛이 모이는 시간
    private const float FlightDuration = 0.32f; // 탄환이 적까지 날아가는 시간
    private const float ImpactDuration = 0.34f; // 명중 섬광이 확장되고 잦아드는 시간
    private readonly RectTransform layer; // 모든 미사일 이미지를 담는 전투 캔버스 층
    private readonly Image cast; // 시전자 앞에 모이는 마법진
    private readonly Image flight; // 앞쪽 머리와 빛 꼬리가 있는 마력탄
    private readonly Image impact; // 명중 좌표에서 한 번 퍼지는 빛
    private bool active; // 실제 카드 연출 중에만 결과 기록
    private Vector2? impactPosition; // 명중한 대상의 실제 화면 좌표

    internal bool IsAttached => layer != null; // 씬이 바뀌기 전 생성한 표시 층의 생존 확인

    internal static bool Matches(CardInstance card) // 원본 마력탄 한 장의 식별자 확인
    { // 대상 카드 확인 시작
        return card?.SourceData?.CardId == "CRD_SHEET_MAGIC_MISSILE"; // 동일한 피해 유형의 다른 카드 제외
    } // 대상 카드 확인 종료

    internal static BattleMagicMissileEffect Create(BattleUnitView actor) // 실전 캔버스의 표시 층 준비
    { // 그림 연결 확인 시작
        Canvas canvas = actor.GetComponentInParent<Canvas>(); // 실제 시전자가 표시되는 캔버스 조회
        Sprite castSprite = Resources.Load<Sprite>(ResourceRoot + "MagicMissileCast"); // 시전 마법진 원화 로드
        Sprite flightSprite = Resources.Load<Sprite>(ResourceRoot + "MagicMissileFlight"); // 비행 탄환 원화 로드
        Sprite impactSprite = Resources.Load<Sprite>(ResourceRoot + "MagicMissileImpact"); // 명중 섬광 원화 로드
        if (canvas == null || castSprite == null || flightSprite == null || impactSprite == null) // 화면과 세 원화의 로드 여부 확인
        { // 원화 준비 실패 처리 시작
            return null; // 기존 피해 연출의 정상 실행 유지
        } // 원화 준비 실패 처리 종료
        return new BattleMagicMissileEffect((RectTransform)canvas.rootCanvas.transform, castSprite, flightSprite, impactSprite); // 실제 전투 화면의 루트 좌표에 설치
    } // 그림 연결 확인 종료

    private BattleMagicMissileEffect(RectTransform canvas, Sprite castSprite, Sprite flightSprite, Sprite impactSprite) // 전투마다 세 원화를 한 번만 생성
    { // 재사용 표시 구성 시작
        layer = (RectTransform)new GameObject("MagicMissileEffects", typeof(RectTransform)).transform; // 실제 캔버스에 붙일 이펙트 층
        layer.SetParent(canvas, false); // 현재 화면에 맞는 좌표와 배율 상속
        layer.anchorMin = Vector2.zero; // 전투 캔버스 왼쪽 아래 기준점
        layer.anchorMax = Vector2.one; // 전투 캔버스 오른쪽 위 기준점
        layer.sizeDelta = Vector2.zero; // 전투 캔버스와 같은 표시 범위
        cast = CreateImage("MagicMissileCast", castSprite, 156f, new Vector2(0.5f, 0.5f)); // 시전자 앞의 마법진 크기와 중심
        flight = CreateImage("MagicMissileFlight", flightSprite, 217f, new Vector2(0.80f, 0.46f)); // 탄환의 발광 머리를 비행 위치에 맞춤
        impact = CreateImage("MagicMissileImpact", impactSprite, 186f, new Vector2(0.5f, 0.5f)); // 명중 지점의 중앙을 기준으로 섬광 확장
    } // 재사용 표시 구성 종료

    internal IEnumerator Travel(BattleUnitView actor, BattleUnitView target) // 시전 위치에서 대상까지 실제 화면 이동
    { // 마력탄 시전과 이동 시작
        Reset(); // 이전 카드에서 숨겨진 이미지와 명중 결과 초기화
        active = true; // 현재 카드의 명중 기록 허용
        layer.SetAsLastSibling(); // 캐릭터와 배경 앞에 장식 그림 표시
        Vector2 actorPoint = ToLocal(actor.EffectAnchor); // 시전자의 현재 원화 위치를 효과 좌표로 변환
        Vector2 targetPoint = ToLocal(target.EffectAnchor); // 대상의 현재 원화 위치를 효과 좌표로 변환
        Vector2 direction = (targetPoint - actorPoint).normalized; // 마법이 날아갈 방향
        Vector2 source = actorPoint + direction * 54f; // 캐릭터 바로 앞에서 마법진 시작
        float heading = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg; // 탄환의 머리를 실제 대상 방향으로 회전
        cast.rectTransform.localPosition = source; // 현재 시전자 앞에 마법진 배치
        yield return Animate(cast, CastDuration, progress => // 전투 속도에 맞춰 마법진 점등
        { // 마법진 프레임 적용 시작
            cast.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.48f, 1.05f, progress); // 시전점에서 작게 시작해 확장
            cast.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-32f, 12f, progress)); // 얇은 마법진의 부드러운 회전
            SetAlpha(cast, Mathf.Sin(Mathf.PI * progress) * 0.82f + 0.10f); // 중앙이 밝아졌다 사라지는 시전 빛
        }); // 마법진 프레임 적용 종료
        flight.rectTransform.localRotation = Quaternion.Euler(0f, 0f, heading); // 도착점 방향으로 탄환 정렬
        yield return Animate(flight, FlightDuration, progress => // 실제 표적까지 마력탄 비행
        { // 마력탄 비행 프레임 시작
            flight.rectTransform.localPosition = Vector2.Lerp(source, targetPoint, progress); // 시전한 위치에서 정해진 적으로 이동
            flight.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.58f, 1f, Mathf.Min(1f, progress * 4f)); // 발사 직후 탄환의 밝은 꼬리 성장
            SetAlpha(flight, 0.94f); // 동굴 배경에서도 읽히는 밝기 유지
        }); // 마력탄 비행 프레임 종료
    } // 마력탄 시전과 이동 종료

    internal void RecordImpact(BattleUnitView target) // 기존 명중 판정에서 통과한 대상만 표시 예약
    { // 실전 대상과 카드 연출 범위 확인 시작
        if (active && target != null) // 현재 비행을 마친 유효한 마력탄과 피격 대상 확인
        { // 실제 명중 좌표 보관 시작
            impactPosition = ToLocal(target.EffectAnchor); // 피격 반응으로 움직이기 전의 맞은 위치
        } // 실제 명중 좌표 보관 종료
    } // 실전 대상과 카드 연출 범위 확인 종료

    internal IEnumerator React() // 정산과 같은 명중 여부로 섬광을 한 번 표시
    { // 섬광 반응 시작
        if (!impactPosition.HasValue) // 빗나가거나 대상이 사라진 경우 확인
        { // 미명중 전투 피드백 대기 시작
            yield return new WaitForSecondsRealtime(GameplaySpeedSettings.BattleDuration(0.20f)); // 기존 빗나감 자막이 보일 짧은 시간 확보
            yield break; // 명중 섬광의 생성 생략
        } // 미명중 전투 피드백 대기 종료
        impact.rectTransform.localPosition = impactPosition.Value; // 실제로 명중한 적의 위치에 섬광 배치
        yield return Animate(impact, ImpactDuration, progress => // 명중 즉시 밝게 확장하고 옅어지는 표시
        { // 명중 프레임 적용 시작
            float expansion = 1f - (1f - progress) * (1f - progress); // 처음 빠르게 퍼진 뒤 느려지는 폭
            impact.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.68f, 1.32f, expansion); // 명중 섬광과 파편을 바깥쪽으로 확장
            SetAlpha(impact, 0.93f * (1f - progress * progress)); // 밝은 중심에서 투명한 끝 프레임으로 감쇠
        }); // 명중 프레임 적용 종료
    } // 섬광 반응 종료

    internal void Reset() // 씬 이동과 카드 중단에서 남은 효과 정리
    { // 활성 단계 해제 시작
        active = false; // 이후 실제 피해 이벤트의 이전 효과 접근 차단
        impactPosition = null; // 이전 명중 좌표 초기화
        if (!IsAttached) // 전투 캔버스가 이미 제거된 경우 확인
        { // 씬 해제 상태 처리 시작
            return; // 제거된 유니티 이미지 참조 생략
        } // 씬 해제 상태 처리 종료
        cast.enabled = false; // 남아 있는 시전 원화 숨김
        flight.enabled = false; // 남아 있는 비행 원화 숨김
        impact.enabled = false; // 남아 있는 명중 원화 숨김
    } // 활성 단계 해제 종료

    private Vector2 ToLocal(Vector3 worldPoint) // 실제 유닛 원화의 월드 위치를 전투 캔버스로 변환
    { // 렌더 위치 변환 시작
        return layer.InverseTransformPoint(worldPoint); // 해상도와 UI 배율에 맞는 표시 좌표
    } // 렌더 위치 변환 종료

    private Image CreateImage(string name, Sprite sprite, float width, Vector2 pivot) // 캐릭터 클릭을 통과하는 장식 이미지 준비
    { // 장식 이미지 구성 시작
        var item = new GameObject(name, typeof(RectTransform), typeof(Image)); // 해당 단계의 투명 UI 스프라이트 생성
        item.transform.SetParent(layer, false); // 카메라와 같은 전투 표시 층에 연결
        Image image = item.GetComponent<Image>(); // 출력 스프라이트와 위치에 접근
        image.sprite = sprite; // 생성된 실제 원화 참조
        image.raycastTarget = false; // 원래 카드와 유닛 클릭 입력을 통과
        image.rectTransform.pivot = pivot; // 시전 중심과 탄환 머리의 실제 기준점
        image.rectTransform.sizeDelta = new Vector2(width, width * sprite.rect.height / sprite.rect.width); // 원화의 실제 비율로 표시 크기 결정
        image.enabled = false; // 마력탄이 사용될 때까지만 이미지 숨김
        return image; // 재사용할 단계별 이미지 반환
    } // 장식 이미지 구성 종료

    private static IEnumerator Animate(Image image, float duration, System.Action<float> render) // 설정한 전투 속도로 프레임별 표시
    { // 시간에 따른 효과 표시 시작
        float adjusted = GameplaySpeedSettings.BattleDuration(duration); // 사용자 전투 속도에 맞춘 단계 시간
        float start = Time.unscaledTime; // 현재 시각을 기준으로 렌더 프레임 진행 계산
        image.enabled = true; // 현재 단계의 원화 한 장 표시
        while (Time.unscaledTime - start < adjusted) // 기본 진행 시간이 끝날 때까지 대기
        { // 현재 화면 프레임 처리 시작
            render((Time.unscaledTime - start) / adjusted); // 시전 및 도착 구간의 현재 진행률 반영
            yield return null; // 선택한 속도에서 다음 실제 화면 렌더 대기
        } // 현재 화면 프레임 처리 종료
        image.enabled = false; // 다음 단계로 이동하기 전 현재 원화 숨김
    } // 시간에 따른 효과 표시 종료

    private static void SetAlpha(Image image, float alpha) // 원화의 자체 알파에 단계 농도 적용
    { // 투명 농도 갱신 시작
        image.color = new Color(1f, 1f, 1f, alpha); // 청백색 원화를 유지하며 농도만 조절
    } // 투명 농도 갱신 종료
} // 마력탄 전투 연출 종료
