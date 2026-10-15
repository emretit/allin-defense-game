#import <UIKit/UIKit.h>

typedef void (*AllinUiAccessibilityChanged)(void);
static id reduceMotionObserver;
static AllinUiAccessibilityChanged reduceMotionChanged;

extern "C" int AllinUiReducedMotion()
{
    return UIAccessibilityIsReduceMotionEnabled() ? 1 : 0;
}

extern "C" void AllinUiObserveReducedMotion(AllinUiAccessibilityChanged changed)
{
    reduceMotionChanged = changed;
    if (reduceMotionObserver != nil) {
        [[NSNotificationCenter defaultCenter] removeObserver:reduceMotionObserver];
        reduceMotionObserver = nil;
    }
    if (changed == nullptr) return;
    reduceMotionObserver = [[NSNotificationCenter defaultCenter]
        addObserverForName:UIAccessibilityReduceMotionStatusDidChangeNotification
        object:nil queue:[NSOperationQueue mainQueue] usingBlock:^(NSNotification *notification) {
            if (reduceMotionChanged != nullptr) reduceMotionChanged();
        }];
}
