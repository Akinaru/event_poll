import 'package:flutter/foundation.dart';

abstract class Result<V extends Object, F extends Object> {
  factory Result.success(V value) => SuccessResult(value);

  factory Result.failure(F failure) => FailureResult(failure);

  bool get isSuccess;
  bool get isFailure;

  V? get value;
  F? get failure;
}

@immutable
class SuccessResult<V extends Object, F extends Object>
    implements Result<V, F> {
  const SuccessResult(this._value);

  final V _value;

  @override
  bool get isSuccess => true;

  @override
  bool get isFailure => false;

  @override
  V get value => _value;

  @override
  F? get failure => null;
}

@immutable
class FailureResult<V extends Object, F extends Object>
    implements Result<V, F> {
  const FailureResult(this._failure);

  final F _failure;

  @override
  bool get isSuccess => false;

  @override
  bool get isFailure => true;

  @override
  V? get value => null;

  @override
  F get failure => _failure;
}

class Unit {
  const Unit._();
}

const unit = Unit._();
