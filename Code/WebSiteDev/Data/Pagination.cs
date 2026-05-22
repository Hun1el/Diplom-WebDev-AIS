using System;

namespace WebSiteDev
{
    /// <summary>
    /// Класс где находится основная логика работы пагинации чтобы легко было использовать на других формах при желании
    /// </summary>
    public class Pagination
    {
        private int totalItems;
        private int pageSize;
        private int currentPage;

        private const int DefaultPageSize = 10;
        private const int MinimumPage = 1;
        private const int EmptyItems = 0;

        public int TotalItems
        {
            get { return totalItems; }
            set
            {
                if (value < 0)
                {
                    totalItems = EmptyItems;
                }
                else
                {
                    totalItems = value;
                }

                if (TotalPages > 0 && currentPage > TotalPages)
                {
                    currentPage = TotalPages;
                    RaisePageChanged();
                }
                else if (TotalPages == 0)
                {
                    currentPage = 0;
                }
            }
        }

        public int PageSize
        {
            get { return pageSize; }
            set
            {
                if (value > 0)
                {
                    pageSize = value;
                }
                else
                {
                    pageSize = MinimumPage;
                }

                if (TotalPages > 0 && currentPage > TotalPages)
                {
                    currentPage = TotalPages;
                    RaisePageChanged();
                }
            }
        }

        public int CurrentPage
        {
            get { return currentPage; }
        }

        public int TotalPages
        {
            get
            {
                if (totalItems <= 0 || pageSize <= 0)
                {
                    return 0;
                }

                double pages = (double)totalItems / pageSize;
                return (int)Math.Ceiling(pages);
            }
        }

        public bool HasPrevious
        {
            get { return currentPage > MinimumPage; }
        }

        public bool HasNext
        {
            get { return currentPage < TotalPages; }
        }

        public event EventHandler PageChanged;

        public Pagination(int totalItems, int pageSize)
        {
            if (totalItems < 0)
            {
                this.totalItems = EmptyItems;
            }
            else
            {
                this.totalItems = totalItems;
            }

            if (pageSize > 0)
            {
                this.pageSize = pageSize;
            }
            else
            {
                this.pageSize = DefaultPageSize;
            }

            currentPage = MinimumPage;
        }

        public void NextPage()
        {
            if (HasNext)
            {
                currentPage = currentPage + 1;
                RaisePageChanged();
            }
        }

        public void PreviousPage()
        {
            if (HasPrevious)
            {
                currentPage = currentPage - 1;
                RaisePageChanged();
            }
        }

        public void GoToPage(int page)
        {
            int targetPage = page;

            if (targetPage < MinimumPage)
            {
                targetPage = MinimumPage;
            }
            else if (TotalPages > 0 && targetPage > TotalPages)
            {
                targetPage = TotalPages;
            }
            else if (TotalPages == 0)
            {
                targetPage = 0;
            }

            if (currentPage != targetPage)
            {
                currentPage = targetPage;
                RaisePageChanged();
            }
        }

        public int GetStartIndex()
        {
            return (currentPage - 1) * pageSize;
        }

        public int GetEndIndex()
        {
            int end = GetStartIndex() + pageSize - 1;

            if (end >= totalItems)
            {
                end = totalItems - 1;
            }

            return end;
        }

        public int GetSkipCount()
        {
            return (currentPage - 1) * pageSize;
        }

        public int GetTakeCount()
        {
            if (totalItems <= 0)
            {
                return 0;
            }

            int skip = GetSkipCount();
            int remaining = totalItems - skip;

            if (remaining < pageSize)
            {
                return remaining;
            }

            return pageSize;
        }

        private void RaisePageChanged()
        {
            if (PageChanged != null)
            {
                PageChanged(this, EventArgs.Empty);
            }
        }
    }
}