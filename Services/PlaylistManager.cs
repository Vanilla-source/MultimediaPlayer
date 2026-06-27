using System;
using System.Collections.Generic;
using System.IO;
using FinalTest.Models;

namespace FinalTest.Services
{
    /// <summary>
    /// 播放列表管理器
    /// 负责播放列表的增删改查操作，以及基于播放模式计算下一首/上一首索引
    ///
    /// 设计思路：
    /// - 内部使用 List<MediaItem> 存储播放列表，维护一个 _currentIndex 表示当前播放位置
    /// - GetNextIndex/GetPreviousIndex 根据当前播放模式（PlayMode）计算下一个索引
    /// - 顺序播放：播完最后一首返回 -1（停止）
    /// - 列表循环：到达末尾后回到第一首（取模运算）
    /// - 单曲循环：始终返回当前索引
    /// - 随机播放：使用 Random 生成随机索引
    /// </summary>
    public class PlaylistManager
    {
        /// <summary>
        /// 内部播放列表存储
        /// </summary>
        private readonly List<MediaItem> _items;

        /// <summary>
        /// 当前播放索引，-1 表示无当前播放项
        /// </summary>
        private int _currentIndex;

        /// <summary>
        /// 随机数生成器，用于随机播放模式
        /// </summary>
        private readonly Random _random;

        /// <summary>
        /// 构造函数：初始化空的播放列表
        /// </summary>
        public PlaylistManager()
        {
            _items = new List<MediaItem>();
            _currentIndex = -1;  // -1 表示无当前项
            _random = new Random();
        }

        // ==================== 属性 ====================

        /// <summary>
        /// 获取播放列表的只读副本
        /// </summary>
        public IReadOnlyList<MediaItem> Items => _items.AsReadOnly();

        /// <summary>
        /// 播放列表中的项目总数
        /// </summary>
        public int Count => _items.Count;

        /// <summary>
        /// 当前播放索引
        /// 设置时自动钳制到有效范围 [-1, Count-1]
        /// </summary>
        public int CurrentIndex
        {
            get => _currentIndex;
            set
            {
                if (_items.Count == 0)
                    _currentIndex = -1;
                else
                    _currentIndex = Math.Max(-1, Math.Min(value, _items.Count - 1));
            }
        }

        /// <summary>
        /// 获取当前正在播放的媒体项，若无则返回 null
        /// </summary>
        public MediaItem CurrentItem
        {
            get
            {
                if (_currentIndex < 0 || _currentIndex >= _items.Count)
                    return null;
                return _items[_currentIndex];
            }
        }

        // ==================== 列表操作 ====================

        /// <summary>
        /// 添加单个媒体文件到播放列表
        /// 如果是第一首个添加的项，自动设为当前播放项
        /// </summary>
        /// <param name="filePath">文件路径</param>
        public void Add(string filePath)
        {
            if (!File.Exists(filePath)) return;

            var item = new MediaItem(filePath);
            _items.Add(item);

            // 首个添加项自动选定
            if (_items.Count == 1)
                _currentIndex = 0;
        }

        /// <summary>
        /// 批量添加媒体文件到播放列表
        /// 使用 IEnumerable<string> 参数，方便与 OpenFileDialog 配合
        /// </summary>
        /// <param name="filePaths">文件路径集合</param>
        public void AddRange(IEnumerable<string> filePaths)
        {
            bool wasEmpty = _items.Count == 0;

            foreach (var path in filePaths)
            {
                if (File.Exists(path))
                {
                    _items.Add(new MediaItem(path));
                }
            }

            // 若原本为空且新增了项，自动选中第一首
            if (wasEmpty && _items.Count > 0)
                _currentIndex = 0;
        }

        /// <summary>
        /// 从播放列表中移除指定索引的项
        /// 自动调整当前索引，保持正确的播放位置
        /// </summary>
        /// <param name="index">要移除的索引</param>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _items.Count) return;

            _items.RemoveAt(index);

            // 调整当前索引的逻辑：
            // 1. 列表变空 → 索引归 -1
            // 2. 删除了当前项之前的项 → 索引前移一位
            // 3. 删除了当前项 → 保持索引（若超出则钳制到末尾）
            if (_items.Count == 0)
            {
                _currentIndex = -1;
            }
            else if (index < _currentIndex)
            {
                _currentIndex--;
            }
            else if (index == _currentIndex)
            {
                _currentIndex = Math.Min(_currentIndex, _items.Count - 1);
            }
        }

        /// <summary>
        /// 清空播放列表中的所有项
        /// </summary>
        public void Clear()
        {
            _items.Clear();
            _currentIndex = -1;
        }

        /// <summary>
        /// 将指定索引的项移动到新的索引位置（拖拽排序）
        /// </summary>
        /// <param name="fromIndex">原始索引</param>
        /// <param name="toIndex">目标索引</param>
        public void MoveItem(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= _items.Count) return;
            if (toIndex < 0 || toIndex >= _items.Count) return;
            if (fromIndex == toIndex) return;

            var item = _items[fromIndex];
            _items.RemoveAt(fromIndex);
            _items.Insert(toIndex, item);

            // 调整当前索引以反映移动
            if (_currentIndex == fromIndex)
            {
                _currentIndex = toIndex;
            }
            else if (fromIndex < _currentIndex && toIndex >= _currentIndex)
            {
                _currentIndex--;
            }
            else if (fromIndex > _currentIndex && toIndex <= _currentIndex)
            {
                _currentIndex++;
            }
        }

        // ==================== 播放导航 ====================

        /// <summary>
        /// 根据当前播放模式计算下一首的索引
        ///
        /// 实现逻辑：
        /// - Sequential：顺序 +1，超出范围返回 -1（停止播放）
        /// - SingleLoop：返回当前索引不变（单曲循环）
        /// - ListLoop：(current+1) % Count 实现循环
        /// - Shuffle：随机生成 [0, Count) 范围内的索引
        /// </summary>
        /// <param name="mode">当前播放模式</param>
        /// <returns>下一首的索引，若无下一首则返回 -1</returns>
        public int GetNextIndex(PlayMode mode)
        {
            if (_items.Count == 0) return -1;

            switch (mode)
            {
                case PlayMode.SingleLoop:
                    // 单曲循环：始终返回当前索引
                    return _currentIndex;

                case PlayMode.ListLoop:
                    // 列表循环：取模实现环形导航
                    return (_currentIndex + 1) % _items.Count;

                case PlayMode.Shuffle:
                    // 随机播放：在有效范围内随机选取
                    // 当列表只有一首时直接返回 0，避免 Random 无意义调用
                    return _items.Count == 1 ? 0 : _random.Next(_items.Count);

                case PlayMode.Sequential:
                default:
                    // 顺序播放：到达末尾返回 -1
                    return (_currentIndex + 1 >= _items.Count) ? -1 : _currentIndex + 1;
            }
        }

        /// <summary>
        /// 根据当前播放模式计算上一首的索引
        /// 逻辑与 GetNextIndex 对称，方向相反
        /// </summary>
        /// <param name="mode">当前播放模式</param>
        /// <returns>上一首的索引，若无上一首则返回 -1</returns>
        public int GetPreviousIndex(PlayMode mode)
        {
            if (_items.Count == 0) return -1;

            switch (mode)
            {
                case PlayMode.SingleLoop:
                    return _currentIndex;

                case PlayMode.ListLoop:
                    // 列表循环：前移时 +Count 再取模确保非负
                    return (_currentIndex - 1 + _items.Count) % _items.Count;

                case PlayMode.Shuffle:
                    return _items.Count == 1 ? 0 : _random.Next(_items.Count);

                case PlayMode.Sequential:
                default:
                    return (_currentIndex - 1 < 0) ? -1 : _currentIndex - 1;
            }
        }

        /// <summary>
        /// 获取播放列表中所有文件路径（用于 OpenFileDialog 的初始目录推断等）
        /// </summary>
        public List<string> GetAllFilePaths()
        {
            var paths = new List<string>();
            foreach (var item in _items)
            {
                paths.Add(item.FilePath);
            }
            return paths;
        }
    }
}
